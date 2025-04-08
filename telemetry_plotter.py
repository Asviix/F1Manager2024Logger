import socket
import time
import json
from multiprocessing import Queue

class TelemetryPlotter:
    def __init__(self, plot_queue: None):
        self.running = True
        self.udp_ip = "127.0.0.1"
        self.udp_port = 20777  # SimHub's default UDP port
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        
    def stop(self):
        self.running = False
        print("\nExiting plotter gracefully...")

    def run(self, queue: Queue):
        try:
            while self.running:
                try:
                    data = queue.get(timeout=1)
                    if not data:
                        continue

                    message = json.dumps({
                        "cars": {
                            "MyTeam1": {
                                "position": data["MyTeam1"].telemetry.driver.position,
                            }
                        }
                    })
                    self.sock.sendto(message.encode('utf-8'), (self.udp_ip, self.udp_port))

                except queue.empty:
                    continue
                except Exception as e:
                    print("Plotter error:", e)
                    time.sleep(0.1)
                    
        except KeyboardInterrupt:
            print("\nExiting plotter gracefully...")
        finally:
            if hasattr(self, 'sock') and self.sock:
                self.sock.close()

    def send_to_simhub(self, var_name, value):
        try:
            message = f"{var_name}:{value}\n"
            self.sock.sendto(message.encode('utf-8'), (self.udp_ip, self.udp_port))
        except Exception as e:
            print(f"Failed to send {var_name}: {e}")