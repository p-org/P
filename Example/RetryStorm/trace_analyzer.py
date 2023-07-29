import glob
import pandas as pd
import matplotlib.pyplot as plt

log_file = glob.glob("PTimeLogs/Log1.csv")[0]

log = pd.read_csv(log_file)

time_steps = sorted(list(set(log.Time.tolist())))

requests = []
server_goodput = []
server_throughput = []

for t in time_steps:
	log_at_t = log[(log.Time == t)]
	send_at_t = log_at_t[(log_at_t.Operation) == "Send"]
	request_sent_at_t = send_at_t[(send_at_t.Event == "PImplementation.eRequest")]
	if not request_sent_at_t.empty:
		requests.append((t, len(request_sent_at_t.index)))
	dequeue_at_t = log_at_t[(log_at_t.Operation) == "Dequeue"]
	server_run_at_t = dequeue_at_t[(dequeue_at_t.Event == "PImplementation.eServerRun")]
	request_dequeued_at_t = dequeue_at_t[(dequeue_at_t.Event == "PImplementation.eRequest")]
	response_dequeued_at_t = dequeue_at_t[(dequeue_at_t.Event == "PImplementation.eResponse")]
	if not server_run_at_t.empty:
		server_goodput.append((t, len(response_dequeued_at_t.index)))
		server_throughput.append((t, len(request_dequeued_at_t.index)))

plt.plot(*zip(*requests), label="Number of Requests")
plt.plot(*zip(*server_goodput), label="Server's Goodput")
plt.plot(*zip(*server_throughput), label="Server's Throughput")
plt.legend()
plt.savefig("trace.pdf", format="pdf", bbox_inches="tight")
