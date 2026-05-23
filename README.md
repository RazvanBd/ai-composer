# ai-composer

Documentația arhitecturală extinsă este disponibilă în
[`ARCHITECTURE.md`](./ARCHITECTURE.md).

## MVP cod (generare din fișiere Markdown)

Am adăugat un MVP de orchestrator care citește artefacte `.md`, construiește context tipizat pe ticket și persistă starea în SQLite.

### Structură

- cod: `src/ai_composer/`
- exemple artefacte: `examples/artifacts/`
- teste: `tests/test_platform.py`

### Rulare CLI

```bash
PYTHONPATH=/home/runner/work/ai-composer/ai-composer/src \
python -m ai_composer.cli \
  --artifacts /home/runner/work/ai-composer/ai-composer/examples/artifacts \
  --output /home/runner/work/ai-composer/ai-composer/output
```

Output-ul include:
- `output/<ticket-id>/context.json` (context strongly typed);
- `.state/orchestrator.db` (state machine persistentă);
- `.state/traces.jsonl` (telemetrie/cost per ticket).