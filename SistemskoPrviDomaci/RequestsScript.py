import requests
import threading
import time
from collections import defaultdict
import random

# Konfiguracija
SERVER_URL = "http://localhost:5050/wordcount"
NUM_FILES = 500 

class ServerTester:
    def __init__(self):
        self.results = defaultdict(int)
        self.lock = threading.Lock()
        self.total_requests = 0
        self.successful_requests = 0
        self.failed_requests = 0
        self.response_times = []
        
    def send_request(self, filename):

        start_time = time.time()
        try:
            response = requests.get(f"{SERVER_URL}?file={filename}", timeout=10)
            elapsed = time.time() - start_time
            
            with self.lock:
                self.total_requests += 1
                self.response_times.append(elapsed)
                
                if response.status_code == 200:
                    self.successful_requests += 1
                else:
                    self.failed_requests += 1
                    print(f"Failed: {filename} - Status {response.status_code}")
                    
        except Exception as e:
            elapsed = time.time() - start_time
            with self.lock:
                self.total_requests += 1
                self.failed_requests += 1
                self.response_times.append(elapsed)
                print(f"Error: {filename} - {str(e)}")
    
    def print_stats(self, test_name, duration):
        print(f"\n{'*'*70}")
        print(f"{test_name} - Rezultati")
        print(f"{'*'*70}")
        print(f"Ukupno zahteva:      {self.total_requests}")
        print(f"Uspesni:             {self.successful_requests}")
        print(f"Neuspesni:           {self.failed_requests}")
        print(f"Trajanje:            {duration:.2f}s")
        print(f"Propusnost:          {self.total_requests/duration:.2f} req/s")
        
        if self.response_times:
            avg_time = sum(self.response_times) / len(self.response_times)
            min_time = min(self.response_times)
            max_time = max(self.response_times)
            print(f"Prosecno vreme odziva:   {avg_time*1000:.2f}ms")
            print(f"Minimalno vreme odziva:   {min_time*1000:.2f}ms")
            print(f"Maksimalno vreme odziva:   {max_time*1000:.2f}ms")
        print(f"{'*'*70}\n")
    
    def reset_stats(self):
        self.total_requests = 0
        self.successful_requests = 0
        self.failed_requests = 0
        self.response_times = []


def test_case_1(num_threads=100):
    
    print("\nTEST 1: CACHE STAMPEDE - Isti fajl")
    print(f"Broj threadova: {num_threads}, Fajl: f1.txt")
    
    tester = ServerTester()
    threads = []
    
    start_time = time.time()
    
    for i in range(num_threads):
        thread = threading.Thread(target=tester.send_request, args=("f1.txt",))
        threads.append(thread)
        thread.start()
    
    for thread in threads:
        thread.join()
    
    duration = time.time() - start_time
    tester.print_stats("TEST 1 - Cache Stampede Prevention", duration)


def test_case_2(num_requests=200):
    
    print(f"\nTEST 2: Razliciti fajlovi sekvencijalno")
    print(f"   Zahteva: {num_requests}, Velicina kesa: 100")
    
    tester = ServerTester()
    
    start_time = time.time()
    
    for i in range(1, num_requests + 1):
        filename = f"f{i}.txt"
        tester.send_request(filename)
    
    duration = time.time() - start_time
    tester.print_stats("TEST 2 - LRU Eviction", duration)


def test_case_3(num_threads=100, requests_per_thread=10):
    
    print(f"\n TEST 3: REALISTIC LOAD - Concurrent + Zipf distribucija")
    print(f"   Threadova: {num_threads}, Zahteva/thread: {requests_per_thread}")
    print(f"   Total zahteva: {num_threads * requests_per_thread}")
    print(f"   Pattern: 80% zahteva za 20% fajlova (real-world)")
    
    tester = ServerTester()
    threads = []
    
    popular_files = [f"f{i}.txt" for i in range(1, 101)]
    all_files = [f"f{i}.txt" for i in range(1, NUM_FILES + 1)]
    
    def worker(thread_id):
        for _ in range(requests_per_thread):
            if random.random() < 0.8:
                filename = random.choice(popular_files)
            else:
                filename = random.choice(all_files)
            
            tester.send_request(filename)
            time.sleep(random.uniform(0.01, 0.05))
    
    start_time = time.time()
    
    for i in range(num_threads):
        thread = threading.Thread(target=worker, args=(i,))
        threads.append(thread)
        thread.start()
    
    for thread in threads:
        thread.join()
    
    duration = time.time() - start_time
    tester.print_stats("TEST 3 - Realistic Load (Zipf)", duration)


def test_case_4(num_bursts=5, threads_per_burst=50, delay_between_bursts=2):
    
    print(f"\nTEST 4: BURST LOAD - Spike testing")
    print(f"   Talasa: {num_bursts}, Threadova/talas: {threads_per_burst}")
    print(f"   Pauza izmedju: {delay_between_bursts}s")
    
    tester = ServerTester()
    all_files = [f"f{i}.txt" for i in range(1, 51)]
    
    overall_start = time.time()
    
    for burst_num in range(num_bursts):
        print(f"\nTalas {burst_num + 1}/{num_bursts}...")
        threads = []
        
        for i in range(threads_per_burst):
            filename = random.choice(all_files)
            thread = threading.Thread(target=tester.send_request, args=(filename,))
            threads.append(thread)
            thread.start()
        
        for thread in threads:
            thread.join()
        
        if burst_num < num_bursts - 1:
            time.sleep(delay_between_bursts)
    
    duration = time.time() - overall_start
    tester.print_stats("TEST 4 - Burst Load", duration)


def main():
    print("-"*60)
    print("\nPokrenut program\n")
    print("-"*60)

    try:
        response = requests.get(f"{SERVER_URL}?file=f1.txt", timeout=5)
        print("Server je dostupan!\n")
    except Exception as e:
        print(f"Server NIJE dostupan: {e}")
        print("    Pokreni server prvo: dotnet run\n")
        return
    
    print("\nIzaberi testove:")
    print("1. Test 1 - Cache Stampede (isti fajl, 100 threadova)")
    print("2. Test 2 - LRU Eviction (200 različitih fajlova)")
    print("3. Test 3 - Realistic Load (Zipf distribucija)")
    print("4. Test 4 - Burst Load (spike testing)")
    print("5. Pokreni SVE testove")
    print("0. Izlaz")
    
    choice = input("\nUnesi broj (0-5): ").strip()
    
    if choice == "1":
        num = int(input("Broj threadova (default 100): ") or "100")
        test_case_1(num)
    
    elif choice == "2":
        num = int(input("Broj zahteva (default 200): ") or "200")
        test_case_2(num)
    
    elif choice == "3":
        threads = int(input("Broj threadova (default 100): ") or "100")
        req_per = int(input("Zahteva po threadu (default 10): ") or "10")
        test_case_3(threads, req_per)
    
    elif choice == "4":
        bursts = int(input("Broj talasa (default 5): ") or "5")
        threads = int(input("Threadova po talasu (default 50): ") or "50")
        test_case_4(bursts, threads)
    
    elif choice == "5":
        test_case_1(100)
        time.sleep(2)
        test_case_2(200)
        time.sleep(2)
        test_case_3(100, 10)
        time.sleep(2)
        test_case_4(5, 50)
    
    elif choice == "0":
        print("Izlaz...")
    else:
        print("Nepostojeca opcija!")
    
    print("\nTestiranje zavrseno!")


if __name__ == "__main__":
    main()