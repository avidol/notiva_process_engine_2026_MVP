UPDATE notifications
SET state = @state,
    rule_violations = @reason
WHERE id = @id;