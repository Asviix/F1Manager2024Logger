import socket
import threading
import time

class TelemetryPlotter:
    def __init__(self, queue):
        self.queue = queue
        self.running = True
        self.udp_ip = "127.0.0.1"
        self.udp_port = 4739  # ✅ SimHub default UDP port for input
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    def start(self):
        threading.Thread(target=self._run, daemon=True).start()

    def _run(self):
        while self.running:
            try:
                data = self.queue.get(timeout=1)
                if data and 'MyTeam1' in data:
                    ocon = data['MyTeam1'].telemetry.driver
                    values = {
                        "Ocon_Speed": ocon.car.speed,
                        "Ocon_Gear": ocon.car.gear,
                        "Ocon_Position": ocon.position,
                        "Ocon_FL_Temp": ocon.car.tyres.temperature.flTemp,
                        "Ocon_Wear": (ocon.car.tyres.wear.flDeg + ocon.car.tyres.wear.frDeg +
                                    ocon.car.tyres.wear.rlDeg + ocon.car.tyres.wear.rrDeg) / 4
                    }

                    for key, value in values.items():
                        self.send_to_simhub(key, value)

            except Exception as e:
                print("Plotter error:", e)
                time.sleep(0.1)

    def send_to_simhub(self, var_name, value):
        message = f"{var_name}:{value}\n"
        self.sock.sendto(message.encode(), (self.udp_ip, self.udp_port))
