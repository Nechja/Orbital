using ErrorOr;

namespace OrbitalDocking.Services.Errors;

public static class DockerErrors
{
    public static class Container
    {
        public static Error NotFound(string containerId) => 
            Error.NotFound("Container.NotFound", $"Container '{containerId}' was not found");
            
        public static Error AlreadyRunning(string containerId) => 
            Error.Conflict("Container.AlreadyRunning", $"Container '{containerId}' is already running");
            
        public static Error AlreadyStopped(string containerId) => 
            Error.Conflict("Container.AlreadyStopped", $"Container '{containerId}' is already stopped");
            
        public static Error NotRunning(string containerId) => 
            Error.Conflict("Container.NotRunning", $"Container '{containerId}' is not running");
            
        public static Error NotPaused(string containerId) => 
            Error.Conflict("Container.NotPaused", $"Container '{containerId}' is not paused");
            
        public static Error InUse(string containerId) => 
            Error.Conflict("Container.InUse", $"Container '{containerId}' is running and cannot be removed");
            
        public static Error OperationFailed(string containerId, string operation) => 
            Error.Failure("Container.OperationFailed", $"Failed to {operation} container '{containerId}'");
    }
    
    public static class Image
    {
        public static Error NotFound(string imageId) => 
            Error.NotFound("Image.NotFound", $"Image '{imageId}' was not found");
            
        public static Error InUse(string imageId) => 
            Error.Conflict("Image.InUse", $"Image '{imageId}' is in use by a container");
            
        public static Error PullFailed(string imageName, string reason) => 
            Error.Failure("Image.PullFailed", $"Failed to pull image '{imageName}': {reason}");
            
        public static Error RemoveFailed(string imageId) => 
            Error.Failure("Image.RemoveFailed", $"Failed to remove image '{imageId}'");
    }
    
    public static class Docker
    {
        public static Error DaemonNotResponding() => 
            Error.Failure("Docker.DaemonNotResponding", "Docker daemon is not responding");
            
        public static Error ConnectionFailed(string reason) => 
            Error.Failure("Docker.ConnectionFailed", $"Failed to connect to Docker: {reason}");
            
        public static Error UnexpectedError(string message) => 
            Error.Unexpected("Docker.UnexpectedError", $"An unexpected error occurred: {message}");
    }
    
    public static class Prune
    {
        public static Error ContainersFailed() => 
            Error.Failure("Prune.ContainersFailed", "Failed to prune containers");
            
        public static Error ImagesFailed() => 
            Error.Failure("Prune.ImagesFailed", "Failed to prune images");
            
        public static Error VolumesFailed() => 
            Error.Failure("Prune.VolumesFailed", "Failed to prune volumes");
    }
}