import re
import json
import subprocess
import argparse
import os
import shutil
import time
import multiprocessing
from constants import configurations, benchmarks, module_names, test_interface_names

PATTERN = re.compile(r'(.*) detected liveness bug in .*\.')

def check_monitors(name, monitors, testcases, interfaces, mod_name):
    # generate test driver
    test_drivers = []
    test_driver_src = []
    test_driver_src.append('module TestMod = ' + '{' + ', '.join(interfaces) + '};\n')
    for test in interfaces:
        test_name = f'PInferGeneratedTest_{test}'
        test_driver_src.append(
f'''test {test_name} [main = {test}]:
    assert {', '.join(monitors)} in (union {mod_name}, TestMod);\n''')
        test_drivers.append(test_name)
    with open(os.path.join('PTst', 'PInferGeneratedTest.p'), 'w') as fd:
        fd.writelines(test_driver_src)

    subprocess.run(['p', 'compile'], stdout=subprocess.DEVNULL)
    print(f'[{name}] Compiled')
    falsified_monitors = set();
    for test in test_drivers:
        if os.path.exists('PCheckerOutput'):
            shutil.rmtree('PCheckerOutput')
        print(f'[{name}] Running test case {test}')
        retcode = subprocess.call(args=['p', 'check', '-tc', test, '-s', '10000'],
                                  stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        if retcode != 0:
            files = os.listdir(os.path.join('PCheckerOutput', 'BugFinding'))
            files = list(filter(lambda x: x.endswith('.json'), files))
            if len(files) == 1:
                f = files[0]
                with open(os.path.join('PCheckerOutput', 'BugFinding', f), 'r') as fd:
                    bug_trace = json.load(fd)
                    prev_ev = None
                    for event in bug_trace:
                        if event['type'] == 'AssertionFailure':
                            print(f'Assertion failed at after {prev_ev["type"]}: {event["details"]["log"]}')
                            if prev_ev == None or prev_ev['type'] != 'MonitorProcessEvent':
                                err = event['details']['error']
                                m = PATTERN.match(err)
                                if m:
                                    failed_monitor = m.group(1).split('.')[-1]
                                    print(f'Failed liveness monitor: {failed_monitor}')
                                    falsified_monitors.add(failed_monitor)
                                    return falsified_monitors
                                else:
                                    print('Failure outside of mined monitors, skipping')
                            else:
                                failed_monitor = prev_ev['details']['monitor']
                                print(f'Failed monitor: {failed_monitor}')
                                falsified_monitors.add(failed_monitor)
                                return falsified_monitors
                        prev_ev = event
            else:
                print(f'Failed to retrive bug finding trace: {len(files)} found')
    return set()

def run_mc_for(benchmark):
    # assumes all test cases are in `PTst`
    os.chdir(benchmark)
    def print_log(msg): print(f'[{benchmark}] {msg}')

    outdir = os.path.join('PGenerated', 'PInfer', 'PInferSpecs')
    if not os.path.exists(outdir) or not os.path.exists(os.path.join(outdir, 'metadata.json')):
        if os.path.exists('PInferOutputs'):
            files = os.listdir('PInferOutputs')
            files.sort()
            latest = files[-1]
            print_log('Generating monitors ...')
            _ = subprocess.run(['p', 'infer', '--action', 'pruning', '-pi', os.path.join('PInferOutputs', latest), '-z3'],
                               stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

    if not os.path.exists(outdir):
        print_log(f'Failed to run mc for {benchmark}: no mined specifications found')
        return
    
    # copy generated specification monitors
    os.system(f'cp -r {outdir} PTst')
    with open(os.path.join('PTst', 'PInferSpecs', 'metadata.json'), 'r') as f:
        metadata = json.load(f)
        active_monitors = [m['name'] for m in metadata]
        falsified_monitors = set()
        checkpoint = os.path.join('PTst', 'PInferCheckpoint.json')
        if os.path.exists(checkpoint):
            with open(checkpoint, 'r') as f:
                checkpoint_data = json.load(f)
                falsified_monitors = set(checkpoint_data['falsified_monitors'])
                active_monitors = [m for m in active_monitors if m not in falsified_monitors]
        
        # check monitors for each configuration, using 100k schedules
        prev_set = set()
        current_set = set(active_monitors)
        start = time.time()
        def save(begin, end):
            with open(checkpoint, 'w') as f:
                json.dump({'falsified_monitors': list(falsified_monitors), 'time': end - begin}, f)
        current_set = current_set - falsified_monitors
        while (prev_set != current_set):
            prev_set = current_set
            print_log(f'Running PChecker on {len(current_set)} monitors ...')
            falsified_this = set(check_monitors(benchmark, current_set, configurations[benchmark], test_interface_names[benchmark], module_names[benchmark]))
            current_set = current_set - falsified_this
            print_log(f'Falsified {len(falsified_this)} invariants')
            falsified_monitors = falsified_monitors.union(falsified_this)
            save(start, time.time())
        print_log(f'Finished in {time.time() - start} seconds')

        with open('prune_after_mc.txt', 'w') as f:
            for v in metadata:
                if v['name'] in current_set:
                    f.write(f'{v["spec"]}\n')

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--benchmarks', type=str, nargs='+', default=benchmarks)

    args = parser.parse_args()
    pool = multiprocessing.Pool(processes=len(args.benchmarks))
    pool.map(run_mc_for, args.benchmarks)
