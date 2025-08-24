namespace OrbitalDocking.Models;

public enum ContainerState
{
    Running,
    Paused,
    Exited,
    Created,
    Restarting,
    Dead,
    Removing
}

public enum DockerAction
{
    Start,
    Stop,
    Restart,
    Remove,
    Pause,
    Unpause,
    Kill,
    Logs,
    Stats,
    Inspect,
    Exec,
    Attach
}

public enum ThemeMode
{
    Light,
    Dark,
    System
}

public enum ImageAction
{
    Pull,
    Push,
    Remove,
    Tag,
    Inspect,
    History,
    Prune
}

public enum VolumeAction
{
    Create,
    Remove,
    Inspect,
    Prune,
    List
}

public enum NetworkAction
{
    Create,
    Remove,
    Connect,
    Disconnect,
    Inspect,
    List
}

public enum ResourceType
{
    Container,
    Image,
    Volume,
    Network
}