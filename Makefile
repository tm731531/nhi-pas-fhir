# nhi-pas-fhir — dev tasks. Run inside the venv (make venv first).
.PHONY: help venv test example lint clean
PY := .venv/bin/python

help:
	@echo "make venv     - create .venv and install dev deps"
	@echo "make test     - run pytest"
	@echo "make example  - build & print a sample TWPAS Bundle (A/B demo)"
	@echo "make clean    - remove caches"

venv:
	python3 -m venv .venv && . .venv/bin/activate && pip install -q --upgrade pip && pip install -q -r requirements-dev.txt
	@echo "venv ready. activate with: . .venv/bin/activate"

test:
	$(PY) -m pytest -q tests/

example:
	$(PY) -m examples.build_sample_bundle

clean:
	find . -type d -name __pycache__ -prune -exec rm -rf {} + ; rm -rf .pytest_cache
