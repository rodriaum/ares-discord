/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

namespace Ares.Common.Util;

public class DockerUtil
{
    public static bool IsRunningInDocker()
    {
        return File.Exists("/.dockerenv") || 
            File.Exists("/proc/1/cgroup") && 
            File.ReadAllText("/proc/1/cgroup").Contains("docker");
    }
}