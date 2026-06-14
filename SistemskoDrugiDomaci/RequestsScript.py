import requests
import threading

SERVER_URL = "http://localhost:5050/"

def send_request(filename):
    try:
        requests.get(f"{SERVER_URL}/{filename}", timeout=10)
    except Exception as e:
        print(f"Error: {e}")

def main():
    num = int(input("Broj zahteva: "))
    filename = input("Naziv fajla: ")

    barrier = threading.Barrier(num)
    threads = []

    def worker():
        barrier.wait()
        send_request(filename)

    for _ in range(num):
        t = threading.Thread(target=worker)
        threads.append(t)

    for t in threads:
        t.start()

    for t in threads:
        t.join()

    print("Zavrseno")

if __name__ == "__main__":
    main()