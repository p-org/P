import os
import argparse
import multiprocessing
import json
from constants import num_traces, benchmarks

configurations = {
    '2PC': ['tcC2P3T3', 'tcC3P4T3', 'tcC3P5T3', 'tcSingleClientNoFailure'],
    'ClockBound': ['tcC1R3', 'tcC2R3', 'tcC3R3', 'tcC4R3', 'tcC3R5'],
    'consensus_epr': ['tcOneNode', 'tcTwoNodes', 'tcThreeNodes', 'tcFiveNodes', 'tcTenNodes'],
    'paxos': ['testBasicPaxos1on1', 'testBasicPaxos3on5', 'testBasicPaxos3on3', 'testBasicPaxos3on1', 'testBasicPaxos2on3', 'testBasicPaxos2on2'],
    'Raft': ['oneClientFiveServersReliable', 'oneClientFiveServersUnreliable', 'twoClientsThreeServersReliable', 'twoClientsThreeServersUnreliable'],
    'ring_leader': ['tcOneNode', 'tcTwoNodes', 'tcThreeNodes', 'tcFiveNodes', 'tcTenNodes'],
    'lockserver': ['tcOneNode', 'tcThreeNodes', 'tcFourNodes', 'tcFiveNodes']
}

event_combs = {
    'Raft': [
        # ('eRequestVote', 'eRequestVoteReply'),
        ('ePutReq', 'eGetReq', 'ePutResp', 'eGetResp', 'eRaftConfig'),
        # ('eAppendEntries', 'eClientGetRequest', 'eClientPutRequest'),
        # ('eEntryApplied', 'eAppendEntries', 'eRaftPutResponse'),
        # ('eNotifyLog',),
        ('eEntryApplied', 'eBecomeLeader', 'eRaftConfig'),
        # ('eEntryApplied', 'eRequestVote', 'eClientPutRequest', 'eRequestVoteReply'),
        # ('eAppendEntriesReply', 'eClientGetRequest'),
        ('eBecomeLeader', 'eRequestVoteReply', 'eRaftConfig'),
        # ('eClientGetRequest', 'eClientPutRequest', 'eRequestVoteReply')
    ]
}

def generate_traces(name: str, trace_dir: str, n_traces: list):
    configs = configurations[name]
    os.chdir(name)
    os.system('p compile')
    prev = None
    for n in sorted(n_traces):
        actual_n = n
        if prev is not None:
            actual_n -= prev
        num_schedules = actual_n // len(configs)
        dest_dir = os.path.join(trace_dir, name, str(n))
        if os.path.exists(dest_dir):
            print(f'{dest_dir} already exists. Skipping...')
            continue
        os.makedirs(dest_dir, exist_ok=True)
        for config in configs:
            if name in event_combs:
                for combo in event_combs[name]:
                    os.system(f'p check --pinfer -tc {config} -ef {" ".join(combo)} -s {num_schedules} -tf {dest_dir}')
            else:
                os.system(f'p check --pinfer -tc {config} -s {num_schedules} -tf {dest_dir}')
        if prev is not None:
            # merge the previously generated traces to here
            prev_dir = os.path.join(trace_dir, name, str(prev))
            curr_dir = os.path.join(trace_dir, name, str(n))
            curr_dir_files = os.listdir(curr_dir)
            for d in os.listdir(prev_dir):
                if not os.path.isdir(os.path.join(prev_dir, d)):
                    continue
                if d in curr_dir_files:
                    print(f'\tMerging {d} for {prev} and {n}')
                    for f in os.listdir(os.path.join(prev_dir, d)):
                        dst_f = f'prev_{f}'
                        os.system(f'cp {os.path.join(prev_dir, d, f)} {os.path.join(curr_dir, d, dst_f)}')
            # finally, modify the metadata
            fpath = os.path.join(curr_dir, 'metadata.json')
            with open (fpath, 'r+') as f:
                metadata = json.load(f)
                for entry in metadata:
                    entry['num_traces'] = n
                f.seek(0)
                json.dump(metadata, f)
        prev = n
    os.chdir('..')

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--trace_dir', type=str, default=os.environ['PINFER_TRACE_DIR'])
    parser.add_argument('--benchmarks', type=str, nargs='+', default=benchmarks)
    parser.add_argument('--num_traces', type=int, nargs='+', default=num_traces)
    args = parser.parse_args()
    print(f'[Step 1] Generating traces for benchmarks: {args.benchmarks}')
    pool = multiprocessing.Pool(processes=len(args.benchmarks))
    pool.starmap(generate_traces, [(name, args.trace_dir, args.num_traces) for name in args.benchmarks])