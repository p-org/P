.PHONY: deps-check test-contracts regression regression-baseline

deps-check:
	python3 scripts/check_dependencies.py

test-contracts:
	bash scripts/run_contract_tests.sh

regression:
	TOKENIZERS_PARALLELISM=false python3 scripts/regression_test.py

regression-baseline:
	TOKENIZERS_PARALLELISM=false python3 scripts/regression_test.py --save-baseline
