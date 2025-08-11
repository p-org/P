#!/usr/bin/env python3

"""
    EXAMPLE USAGE:
        python evaluate_chatbot.py --metric pass_at_k -k 1 -n 1 -t 0.9 --trials 5 --benchmark-dir evaluation/p-model-benchmark
"""

import sys
import argparse
import os
import evaluation.metrics.pass_at_k as pass_at_k
import evaluation.visualization.viz_pass_at_k as viz_pass_at_k
import traceback
import json
from datetime import datetime
import random
import tests.pipeline.pipeline_tests as tests
from core.pipelining.prompting_pipeline import PromptingPipeline
from compute_metrics import compute_average_token_usage
import time

CONFIG_MAX_TEMP = 1.2
CONFIG_MODEL = "us.anthropic.claude-3-7-sonnet-20250219-v1:0"

TEST_SETS = {
    'base_old': [tests.taskgen_base, tests.test_base_old, tests.oracle_base],
    'base_all_docs': [tests.taskgen_base, tests.test_base_all_docs, tests.oracle_base],
    'base_few_shot': [tests.taskgen_base, tests.test_base_few_shot, tests.oracle_base],
    'base_RAG1000_inline': [tests.taskgen_base, tests.test_base_RAG1000_inline, tests.oracle_base],
    'base_RAG2000_inline': [tests.taskgen_base, tests.test_base_RAG2000_inline, tests.oracle_base],
    'base_RAG2000_inline_fewshot': [tests.taskgen_base, tests.test_base_RAG2000_inline_fewshot, tests.oracle_base],
    'base_RAG2000_asdoc': [tests.taskgen_base, tests.test_base_RAG2000_asdoc, tests.oracle_base],
    'base_RAG2000_inline_aMLML12v2': [tests.taskgen_base, tests.test_base_RAG2000_inline_aMLML12v2, tests.oracle_base],
    'dd2proj_legacy': [tests.taskgen_dd2proj, tests.test_dd2proj_legacy, tests.oracle_dd2proj],
    'dd2proj_replicated': [tests.taskgen_dd2proj, tests.test_dd2proj_replicated, tests.oracle_dd2proj_replicated],
    'dd2psrc': [tests.test_taskgen_dd2psrc, tests.test_dd2proj_psrc, tests.oracle_dd2psrc_correctness],
    'dd2proj_current': [tests.taskgen_dd2proj, tests.test_dd2proj_current, tests.oracle_dd2proj_current],
    'pchecker_fix_basic_one': [tests.taskgen_pchecker_fix_single, tests.test_fix_pchecker_errors, tests.oracle_fix_pchecker_errors],
    'pchecker_fix_basic_full': [tests.taskgen_pchecker_fix, tests.test_fix_pchecker_errors, tests.oracle_fix_pchecker_errors],
}

CURRENT_TEST_NAME = "pchecker_fix_basic_full"
CURRENT_TEST_SET = TEST_SETS[CURRENT_TEST_NAME]
TASKGEN, PIPELINE, ORACLE = CURRENT_TEST_SET

def process_args():
    parser = argparse.ArgumentParser(description="Evaluate chatbot using different metrics")
    parser.add_argument("--metric", type=str, choices=list(METRIC_HANDLERS.keys()), required=True,
                      help="Which metric to compute")
    parser.add_argument("-k", type=int, help="Value for k in pass@k metric")
    parser.add_argument("-t", type=str, help="Temperature to be used for the model. 'random' for random sampling")
    parser.add_argument("-n", type=int, help="Number of samples")
    parser.add_argument("--trials", type=int, help="Number of trails", default=1)
    parser.add_argument("--benchmark-dir", type=str, help="Path to the benchmark directory")
    parser.add_argument("--out-dir", type=str, help="Path to the output directory where results will be stored", default="results")
    
    return parser.parse_args()

def model_caller(task, **kwargs):

# ---------------------------------------------------------
    # pipeline = tests.test_interactive_one_pass_new(
    #     task=task,
    #     model=CONFIG_MODEL,
    #     **kwargs
    # )
# ---------------------------------------------------------
    pipeline = PIPELINE(
        task=task,
        model=CONFIG_MODEL,
        **kwargs
    )



    # ======= TO REPLAY A PREVIOUS RUN ======================
    # test_name, _ = task
    # trial = kwargs['trial']
    # result_dir = "key-results/2025-06-24-15-04-43"
    # with open(f"{result_dir}/trial_{trial}/{test_name}/conversation.json", "r") as f:
    #    conversation = json.load(f)

    # with open(f"{result_dir}/trial_{trial}/{test_name}/token_usage.json", "r") as f:
    #    token_usage = json.load(f)

    # pipeline = PromptingPipeline()
    # pipeline.conversation = conversation
    # pipeline.usage_stats = token_usage
    # ========================================================
    return pipeline

def is_float(t):
    try:
        float(t)
        return True
    except:
        return False

def compute_pass_at_k(args, out_dir=None, **kwargs):
    tasks = TASKGEN(args.benchmark_dir)
    
    temp = float(args.t) if is_float(args.t) else random.uniform(0, CONFIG_MAX_TEMP)
    return pass_at_k.compute(
                                model_caller, 
                                lambda n, out: ORACLE(n, out, out_dir=out_dir), 
                                args.k, 
                                args.n, 
                                temp, 
                                tasks,
                                out_dir=out_dir, 
                                **kwargs
                            )

def compute_avg_p_at_k(results):
    sum_p_at_k = 0
    for _, p_at_k in results:
        sum_p_at_k += p_at_k
    avg_p_at_k = sum_p_at_k/len(results)
    return avg_p_at_k

def assert_same_keys(dict_list, msg):
    if not dict_list:
        return
        
    reference_keys = set(dict_list[0].keys())
    
    assert all(set(d.keys()) == reference_keys for d in dict_list), msg


def compute_avg_passrate_per_subtest(subtest_dict, totals):
    avg_dict = {}
    for subtest_name, summed in subtest_dict.items():
        avg_dict[subtest_name] = summed/totals[subtest_name]

    return avg_dict

def initialize_subdicts(sum_passrate_dict, one_test_dict):
    for test_name in sum_passrate_dict:
        for oracle in one_test_dict:
            sum_passrate_dict[test_name] = {**sum_passrate_dict[test_name], oracle:0}
        
    return sum_passrate_dict

def compute_avg_passrate_per_test(results):
    all_trial_dicts = [ d for d,_ in results ]
    assert_same_keys(all_trial_dicts, "[compute_avg_passrate_per_test] All trials must have the same set of tests!")
    
    # sample value, all_trial_dicts = [{'1_lightswitch': {'compile': True, 'tcSingleSwitchLight': False, 'tcMultipleSwitchesOneLight': False, 'tcMultipleSwitchesAndLights': False}}, {'1_lightswitch': {'compile': True, 'tcToggleOffToOn': False, 'tcToggleOnToOff': False, 'tcMultipleSwitchesSameLight': False, 'tcAllScenarios': False}}]
    # tests_from_sample_trial = [d for _,d in all_trial_dicts[0].items()]
    # assert_same_keys(tests_from_sample_trial, "[compute_avg_passrate_per_test] Each test in a trial must have the same set of oracles!")
    
    # sum_passrate_dict = { k:{} for k,_ in all_trial_dicts[0].items() }
    # sum_passrate_dict = initialize_subdicts(sum_passrate_dict, tests_from_sample_trial[0])
    sum_passrate_dict = {}
    totals = {}
    for result_dict, _ in results:
        for test_name, subtest_dict in result_dict.items():
            if test_name not in sum_passrate_dict:
                sum_passrate_dict[test_name] = {}
                totals[test_name] = {}
            for subtest_name in subtest_dict:
                if subtest_name not in sum_passrate_dict[test_name]:
                    sum_passrate_dict[test_name][subtest_name] = 0
                    totals[test_name][subtest_name] = 0

                sum_passrate_dict[test_name][subtest_name] += 1 if result_dict[test_name][subtest_name] else 0
                totals[test_name][subtest_name] += 1
                
    avg_passrate_dict = { k:compute_avg_passrate_per_subtest(v,totals[k]) for k,v in sum_passrate_dict.items() }
    return avg_passrate_dict

def construct_lines(key, subdict):
    lines =[f"{(value * 100):.2f}% : {key}->{subkey} avg. pass rate" for subkey, value in subdict.items()]
    return "\n".join(lines) 

def pp_avg_passrate_per_test(results_dict):
    formatted_lines = [
        construct_lines(key, subdict)
        for key, subdict in sorted(results_dict.items(), key=lambda x: x[0])
    ]
    
    return "\n".join(formatted_lines)

def pretty_print_report(report):

    k = report['args']['k']
    n = report['args']['n']
    t = report['args']['t']
    trials = report['args']['trials']
    avg_p_at_k = report['results']['avg_p_at_k']
    avg_pass_rates = report['results']['avg_pass_rates']

    lines = []
    lines.append("---- RESULTS SUMMARIZED ----")
    lines.append(f"pass@{k}(n={n},t={t},trials={trials}) = {avg_p_at_k} (avg)")
    lines.append(pp_avg_passrate_per_test(avg_pass_rates))

    return "\n".join(lines)


def save_report_json(report, save_dir):
    os.makedirs(save_dir, exist_ok=True)
    filename = f"report.json"
    filepath = os.path.join(save_dir, filename)
    
    with open(filepath, 'w') as f:
        json.dump(report, f, indent=4)
    
    return filepath

def process_p_at_k_results(args, results, save_dir="/tmp", **external_metrics):

    avg_p_at_k = compute_avg_p_at_k(results)
    avg_pass_rates = compute_avg_passrate_per_test(results)


    report = {
        "name": CURRENT_TEST_NAME,
        "args": {
            **vars(args)
        },

        "results": {
            "avg_p_at_k": avg_p_at_k,
            "avg_pass_rates": avg_pass_rates,
            **external_metrics
        }
    }

    print(results)
    print(pretty_print_report(report))

    saved_json = save_report_json(report, save_dir)
    print(f"Report saved to {saved_json}")
    return saved_json
    
METRIC_HANDLERS = {
    "pass_at_k": (compute_pass_at_k, process_p_at_k_results, viz_pass_at_k.visualize_json_results),
}

def write_and_return_result(result, filepath):
    result_dict, _ = result
    with open(filepath, 'w') as f:
        json.dump(result_dict, f, indent=4)
    
    return result

def main():
    args = process_args()
    timestamp = datetime.now().strftime('%Y-%m-%d-%H-%M-%S')
    out_dir = f"{args.out_dir}/{args.metric}/{timestamp}"
    
    handler, result_processor, visualizer = METRIC_HANDLERS[args.metric]

    try:
        # Time the list comprehension
        start_time = time.time()
        results = [
                write_and_return_result(handler(
                        args, 
                        out_dir=f"{out_dir}/trial_{trial}", 
                        trial=trial,
                    ), f"{out_dir}/trial_{trial}/result.json")
                for trial in range(args.trials)
            ]
        
        end_time = time.time()
        total_exec_time = end_time - start_time
        avg_exec_time = total_exec_time/args.trials
        
        report_json = result_processor(
                args, 
                results, 
                save_dir=out_dir, 
                avg_exec_time=avg_exec_time, 
                total_exec_time=total_exec_time,
            )
        visualizer(report_json, "p_at_k.png")

        if args.metric == "pass_at_k":
            compute_average_token_usage(out_dir)
        
        with open(f"{out_dir}/{CURRENT_TEST_NAME}.txt", "w") as f:
            f.write(CURRENT_TEST_NAME)

    except Exception as e:
        print(f"Error running {args.metric}: {e}")
        traceback.print_exc()
        sys.exit(1)

if __name__ == "__main__":
    main()
