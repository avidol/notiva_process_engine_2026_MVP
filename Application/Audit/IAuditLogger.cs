using ProcessEngine.Worker.Domain.Audit;

namespace ProcessEngine.Worker.Application.Audit;

public interface IAuditLogger
{
    void Log(AuditEvent auditEvent);
}
