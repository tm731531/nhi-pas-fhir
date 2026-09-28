# Shared, language-agnostic tasks (validation is language-independent).
.PHONY: fetch-validator validate
fetch-validator:  ## download IG package + HL7 validator into .fhir/ (gitignored)
	bash tools/fetch_validation_assets.sh
validate:  ## validate any FHIR json against the pinned IG:  make validate FILE=path.json
	bash tools/validate.sh $(FILE)
