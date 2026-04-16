using DesignGuard.Data.Mongo.Documents;
using DesignGuard.Models;

namespace DesignGuard.Data.Mongo;

public static class ProjectDocumentMapper
{
    public static ProjectModel ToModel(ProjectDocument d) =>
        ProjectMapper.ToModel(ProjectDocumentEntityConverter.ToEntity(d));
}
