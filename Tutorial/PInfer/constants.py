import os

num_traces = [
    500, 1000, 5000, 10000, 50000, 100000
]

benchmarks = [
    '2PC', 'ClockBound', 'consensus_epr', 'paxos', 'Raft', 'ring_leader'
]

config_events = {
    'paxos': 'ePaxosConfig',
    '2PC': 'eMonitor_AtomicityInitialize'
}

trace_dir = os.environ['PINFER_TRACE_DIR']