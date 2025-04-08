import socket
import time
import json
import configparser
from multiprocessing import Queue
from queue import Empty

config = configparser.ConfigParser()
config.read('settings.ini')

cars = [
    "Ferrari1", "Ferrari2",
    "McLaren1", "McLaren2",
    "RedBull1", "RedBull2",
    "Mercedes1", "Mercedes2",
    "Alpine1", "Alpine2",
    "Williams1", "Williams2",
    "Haas1", "Haas2",
    "RacingBulls1", "RacingBulls2",
    "KickSauber1", "KickSauber2",
    "AstonMartin1", "AstonMartin2",
    "MyTeam1", "MyTeam2"
]

class TelemetryPlotter:
    def __init__(self, plot_queue: None):
        self.running = True
        self.udp_ip = config.get("UDP", "IP")
        self.udp_port = int(config.get("UDP", "PORT"))
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        
    def stop(self):
        self.running = False

    def run(self, queue: Queue):
        try:
            while self.running:
                try:
                    data = queue.get(timeout=1)
                    if not data:
                        continue

                    session_data = {}                 
                    cars_data = {}
                    for car_name in cars:
                        if car_name in data and hasattr(data[car_name], 'telemetry'):
                            session_data = {
                                "timeElapsed": data[car_name].telemetry.session.timeElapsed,
                                "trackName": data[car_name].telemetry.session.trackName,
                                "bestSessionTime": data[car_name].telemetry.session.bestSessionTime,
                                "rubberState": data[car_name].telemetry.session.rubber,
                                "airTemp": data[car_name].telemetry.session.weather.airTemp,
                                "trackTemp": data[car_name].telemetry.session.weather.trackTemp,
                                "weather": data[car_name].telemetry.session.weather.weather,
                            }
                            try:
                                cars_data[car_name] = {
                                    "position": data[car_name].telemetry.driver.position
                                }
                            except AttributeError:
                                continue  # Skip if data structure is incomplete
                    if cars_data:  # Only send if we have valid data
                        message = json.dumps({"cars": cars_data, "session": session_data})
                        self.sock.sendto(message.encode('utf-8'), (self.udp_ip, self.udp_port))

                except Empty:
                    continue
                except Exception as e:
                    print("Plotter error:", e)
                    time.sleep(0.1)
            time.sleep(1)        
        finally:
            if hasattr(self, 'sock') and self.sock:
                self.sock.close()

    def send_to_simhub(self, var_name, value):
        try:
            message = f"{var_name}:{value}\n"
            self.sock.sendto(message.encode('utf-8'), (self.udp_ip, self.udp_port))
        except Exception as e:
            print(f"Failed to send {var_name}: {e}")