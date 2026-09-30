# AI Assistance Policy

This project is developed with substantial AI assistance (Claude). We are transparent about
that, and we hold AI-generated content to the same bar as everything else here:

- **Medical correctness is machine-proven, not trusted.** Every FHIR artifact must pass the
  official HL7 validator at 0 errors and match the byte-for-byte golden fixtures — AI output
  included. "It looks right" / "it compiles" is never sufficient.
- **No fabricated FHIR values.** Values are transcribed from the authoritative IG package;
  unverified detail is a `TODO`, not a guess.
- **CQL verdicts come from the official rules**, executed by a faithful engine — not from the
  AI's or our own re-interpretation of the rules.
- **No real patient data** is ever used, by humans or AI.

How we back this up in practice — the layered, machine-proven test strategy (external validator +
byte-for-byte goldens + contract/integration tests + independent adversarial AI review passes) — is
documented in **[TESTING.md](TESTING.md)**.

If you find anything that violates the above, please open an issue — that is exactly the kind
of error this policy exists to catch.
