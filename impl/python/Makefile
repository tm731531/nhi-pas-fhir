# Python implementation tasks. Validation is the shared root target (make -C ../.. validate).
.PHONY: venv test example validate
PY := .venv/bin/python
venv:
	python3 -m venv .venv && . .venv/bin/activate && pip install -q --upgrade pip && pip install -q -r requirements-dev.txt && pip install -q -e .
test:
	$(PY) -m pytest -q tests/
example:
	$(PY) -m examples.build_pa_bundle
validate: example  ## build then validate against the pinned IG (uses root tools)
	bash ../../tools/validate.sh build/pa-bundle.json
