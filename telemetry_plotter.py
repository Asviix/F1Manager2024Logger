import time
import queue
from multiprocessing import Queue

def run_telemetry_plotter(plot_queue: Queue):
    """Minimal plot queue consumer"""
    while True:
        try:
            plot_queue.get(timeout=1)  # Just clear the queue
            plot_queue.task_done()  # Mark items as processed
        except queue.Empty:
            time.sleep(0.1)