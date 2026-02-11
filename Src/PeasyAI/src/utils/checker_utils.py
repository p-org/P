import subprocess
from utils import file_utils
import os
from datetime import datetime
from glob import glob

def run_pchecker_test_case(test_name, project_path, schedules=100, timeout_seconds=20, seed="default-seed"):
    cmd = ['p', 'check', '-tc', test_name, '-s', str(schedules), "--seed", str(hash(seed) & 0xffffffff)]
    print(" ".join(cmd))
    try:
        result = subprocess.run(cmd, capture_output=True, cwd=project_path, timeout=timeout_seconds)
        return result.returncode == 0, result
    except Exception as e:
        return False, None

def starts_with_letter(s):
    stripped  = s.strip()
    return stripped and stripped[0].isalpha()

def discover_tests(project_path):
    cmd = ['p', 'check', '--list-tests']
    result = subprocess.run(cmd, capture_output=True, cwd=project_path)
    lines = result.stdout.decode('utf-8').split("\n")
    return list(filter(lambda l: starts_with_letter(l), lines))

def try_pchecker(project_path, captured_streams_output_dir=None, schedules=100, timeout=20, seed="default-seed"):

    if not captured_streams_output_dir:
        timestamp = datetime.now().strftime('%Y-%m-%d-%H-%M-%S')
        captured_streams_output_dir = f"/tmp/checker-utils/{timestamp}"

    tests = discover_tests(project_path)
    results = {}
    trace_dicts = {}
    trace_logs = {}

    for test_name in tests:
        is_pass, result_obj = run_pchecker_test_case(test_name, project_path, schedules=schedules, timeout_seconds=timeout, seed=seed)
        out_dir = f"{captured_streams_output_dir}/{test_name}" if captured_streams_output_dir else None
        
        if out_dir:
            os.makedirs(out_dir, exist_ok=True)

        file_utils.write_output_streams(result_obj, out_dir)
        results[test_name] = is_pass
        if not is_pass:
            if result_obj:
                bug_finding_dir = f"{project_path}/PCheckerOutput/BugFinding"
                trace_dict_file = glob(f"{bug_finding_dir}/*.trace.json")[0]
                trace_log_file = glob(f"{bug_finding_dir}/*_0_0.txt")[0]
                trace_dicts[test_name] = file_utils.read_json_file(trace_dict_file)
                trace_logs[test_name] = file_utils.read_file(trace_log_file)
                if out_dir:
                    file_utils.copy_file(trace_dict_file, f"{out_dir}/trace.json")
                    file_utils.copy_file(trace_log_file, f"{out_dir}/trace.txt")
            else: # This means that the checker timed out
                trace_dicts[test_name] =   [{
                                            "type": "PChecker Timed Out",
                                            "details": {
                                                "log": "",
                                                "error": "",
                                                "payload": ""
                                                }
                                            }]
                trace_logs[test_name] = "<ErrorLog> Checker Timed Out\n"
                if out_dir:
                    file_utils.write_file(f"{out_dir}/trace.txt", trace_logs[test_name])
                    file_utils.write_file(f"{out_dir}/trace.json", f"{trace_dicts[test_name]}")


    return results, trace_dicts, trace_logs


def try_pchecker_on_dict(project_state):
    timestamp = datetime.now().strftime('%Y-%m-%d-%H-%M-%S')
    out_dir = f"/tmp/checker-utils/{timestamp}"
    file_utils.write_project_state(project_state, out_dir)
    return try_pchecker(out_dir)