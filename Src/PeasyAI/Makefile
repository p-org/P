.PHONY: deps-check test-contracts test-unit test regression regression-baseline

deps-check:
	python3 scripts/check_dependencies.py

test-contracts:
	bash scripts/run_contract_tests.sh

test-unit:
	PYTHONPATH=src python3 -m pytest tests/ -v --ignore=tests/pipeline

test: test-unit test-contracts

regression:
	TOKENIZERS_PARALLELISM=false python3 scripts/regression_test.py

regression-baseline:
	TOKENIZERS_PARALLELISM=false python3 scripts/regression_test.py --save-baseline
