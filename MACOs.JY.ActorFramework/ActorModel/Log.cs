﻿using NLog;
using NLog.Config;
using NLog.LayoutRenderers;
using System.Text;

namespace MACOs.JY.ActorFramework
{
    [LayoutRenderer("buildConfiguration")]
    [ThreadAgnostic]
    internal class BuildConfigLayoutRender : LayoutRenderer
    {
        private string buildconfig;

        private string GetBuildConfig()
        {
            if (buildconfig != null)
            {
                return buildconfig;
            }
#if DEBUG
            buildconfig = "Debug";
#else
            buildconfig = "Release";
#endif
            return buildconfig;
        }

        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            builder.Append(GetBuildConfig());
        }
    }
}