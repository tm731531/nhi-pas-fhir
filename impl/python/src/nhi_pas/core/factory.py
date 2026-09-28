"""AssemblerFactory — pick the right case assembler by (ig, case_type). Callers stay generic."""
from __future__ import annotations

from .interfaces import ICaseAssembler, PACase


class AssemblerFactory:
    _registry: dict[tuple[str, str], type[ICaseAssembler]] = {}

    @classmethod
    def register(cls, ig: str, case_type: str):
        def deco(a: type[ICaseAssembler]):
            cls._registry[(ig, case_type)] = a
            a.ig, a.case_type = ig, case_type
            return a
        return deco

    @classmethod
    def for_case(cls, case: PACase) -> ICaseAssembler:
        key = (case.ig, case.case_type)
        if key not in cls._registry:
            raise KeyError(f"no assembler registered for {key}; registered: {list(cls._registry)}")
        return cls._registry[key]()

    @classmethod
    def registered(cls) -> list[tuple[str, str]]:
        return list(cls._registry)
