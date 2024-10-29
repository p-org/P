import os

num_traces = [
    500, 1000, 2000, 4000, 8000, 10000, 20000
]

benchmarks = [
    '2PC', 'ClockBound', 'consensus_epr', 'paxos', 'Raft', 'ring_leader', 'lockserver'
]

config_events = {
    'paxos': 'ePaxosConfig',
    '2PC': 'eMonitor_AtomicityInitialize'
}

trace_dir = os.environ['PINFER_TRACE_DIR']