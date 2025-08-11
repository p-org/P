def compute_total_tests(results):
    total = 0
    for test_name, subtest_dict in results.items():
        total += len(subtest_dict.keys())
    return total

def compute_pass_at_k_value(results):
    total = compute_total_tests(results)

    passed = 0
    for test_name, subtest_dict in results.items():
        for subtest, has_passed in subtest_dict.items():
            passed += 1 if has_passed else 0

    return passed/total


def compute(model_caller, oracle, k, n, t, tasks, **kwargs):
    print("==== COMPUTING PASS@K ====")
    print(f"k = {k}")
    print(f"n = {n}")
    print("==========================")

    final_results = {} # "<test_name>": [pass, fail, pass, fail....]
    total_tasks = len(tasks)
    for i, task in enumerate(tasks):
        test_name, *_ = task
        final_results[test_name] = []
        llm_result = model_caller(task, temperature=t, k=k, n=n, task_number=i, total_tasks=total_tasks, **kwargs)
        
        # The oracle may run several kinds of tests, e.g. multiple P test cases
        # So the return value is a dictionary of <subtest_name: str>: <pass: bool>
        test_result = oracle(task, llm_result)
        final_results[test_name] = {**test_result}
    
    p_at_k = compute_pass_at_k_value(final_results)
    return final_results, p_at_k
