import csv
import glob
import numpy as np
import pandas as pd

# Finds the mean latency of the given event.
def find_mean_event_latency_in_log(log):
    send_operations = log[(log.Operation == "Send")]
    dequeue_operations = log[(log.Operation == "Dequeue")]
    send_operations_of_event = send_operations[(send_operations.Event == "PImplementation.eReadRequest")]
    dequeue_operations_of_event = dequeue_operations[(dequeue_operations.Event == "PImplementation.eReadResponse")]
    send_operations_of_event.drop(send_operations_of_event.tail(len(send_operations_of_event.index) - len(dequeue_operations_of_event.index)).index, inplace = True)
    return dequeue_operations_of_event.Time.mean() - send_operations_of_event.Time.mean()

def find_mean_event_latency(logs):
    mean_latencies = pd.DataFrame([find_mean_event_latency_in_log(log) for log in logs])[0]
    return mean_latencies.mean()

log_files = glob.glob("PCheckerOutput/TimedLogs/Log*.csv")

logs = [pd.read_csv(f) for f in log_files]

print(find_mean_event_latency(logs))
