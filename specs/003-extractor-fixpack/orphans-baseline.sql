-- The codemem_orphans statement MemOS feature 059 shipped (MemOS.Data/CodeMem/SqliteCodeMemSymbolRepository.vb,
-- OrphansSqlTemplate), with its three placeholders substituted so it runs directly against a map file
-- (no ATTACH; the "codemem." schema prefix removed):
--   {0} the solution id(s) in scope        -> :solution_id below (one solution per run)
--   {1} the unexamined kinds               -> 'namespace', 'project', 'field', 'property'
--   {2} the type kinds exempt from the implements rule -> 'class', 'structure', 'module', 'interface'
-- Dev-time artefact of fixpack 003 (research R37): reproduces the 059 close-out baseline to the row
-- (GameRoom 306 = 97 + 209; CodeMem 125 = 3 + 1 + 2 + 119, read 2026-09-13) and records the movement after
-- the live-map runs. Run read-only (mode=ro). The grouped form below counts per project and kind; replace the
-- SELECT list with 059's columns to list the rows themselves.

WITH RECURSIVE containment(member_id, container_id) AS (
    SELECT s.id, s.container_id FROM code_symbols s
    WHERE s.is_active = 1 AND s.container_id IS NOT NULL AND s.solution_id IN (:solution_id)
    UNION ALL
    SELECT c.member_id, p.container_id FROM containment c
    JOIN code_symbols p ON p.id = c.container_id
    WHERE p.is_active = 1 AND p.container_id IS NOT NULL
),
referenced(symbol_id) AS (
    SELECT e.target_symbol_id FROM code_edges e
    WHERE e.solution_id IN (:solution_id)
      AND e.verb <> 'part_of'
      AND e.target_symbol_id IS NOT NULL
      AND e.source_symbol_id <> e.target_symbol_id
      AND NOT EXISTS (SELECT 1 FROM containment i WHERE i.member_id = e.source_symbol_id AND i.container_id = e.target_symbol_id)
    UNION
    SELECT m.container_id FROM containment m
    JOIN code_edges e ON e.target_symbol_id = m.member_id AND e.verb <> 'part_of'
    WHERE e.source_symbol_id <> m.container_id
      AND NOT EXISTS (SELECT 1 FROM containment i WHERE i.member_id = e.source_symbol_id AND i.container_id = m.container_id)
)
SELECT s.kind, p.name AS project_name, COUNT(*) AS orphans
FROM code_symbols s
LEFT JOIN code_symbols p ON p.id = s.project_symbol_id AND p.is_active = 1
WHERE s.is_active = 1
  AND s.solution_id IN (:solution_id)
  AND s.kind NOT IN ('namespace', 'project', 'field', 'property')
  AND s.id NOT IN (SELECT symbol_id FROM referenced)
  AND NOT EXISTS (SELECT 1 FROM code_edges h WHERE h.source_symbol_id = s.id
                  AND (h.verb = 'handles' OR (h.verb = 'implements' AND s.kind NOT IN ('class', 'structure', 'module', 'interface'))))
GROUP BY s.kind, p.name
ORDER BY p.name, s.kind;
