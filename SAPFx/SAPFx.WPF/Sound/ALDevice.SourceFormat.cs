using System;
using static OpenAL.AL10;
using static OpenAL.ALEXT;

namespace SAPFx.WPF.Sound
{
	partial class ALDevice
	{
		public static class SourceFormat
		{
			// invalid
			public static int NONE { get; } = AL_NONE;

			// normal
			public static int MONO_8 { get; } = AL_FORMAT_MONO8;
			public static int MONO_16 { get; } = AL_FORMAT_MONO16;
			public static int STEREO_8 { get; } = AL_FORMAT_STEREO8;
			public static int STEREO_16 { get; } = AL_FORMAT_STEREO16;

			// AL_EXT_FLOAT32
			public static int MONO_32 { get; } = AL_FORMAT_MONO_FLOAT32;
			public static int STEREO_32 { get; } = AL_FORMAT_STEREO_FLOAT32;

			// AL_EXT_MCFORMATS
			public static int MULTI_51CH_N8 { get; private set; }
			public static int MULTI_51CH_N16 { get; private set; }
			public static int MULTI_71CH_N8 { get; private set; }
			public static int MULTI_71CH_N16 { get; private set; }

			// AL_EXT_MCFORMATS + AL_EXT_FLOAT32
			public static int MULTI_51CH_N32 { get; private set; }
			public static int MULTI_71CH_N32 { get; private set; }

			// Status
			public static bool IsAllow_FLOAT32 { get; private set; }
			public static bool IsAllow_MCFORMATS { get; private set; }

			static SourceFormat()
			{
				IsAllow_FLOAT32 = alIsExtensionPresent("AL_EXT_FLOAT32");
				IsAllow_MCFORMATS = alIsExtensionPresent("AL_EXT_MCFORMATS");

				MULTI_51CH_N8 = IsAllow_MCFORMATS ? alGetEnumValue("AL_FORMAT_51CHN8") : -1;
				MULTI_51CH_N16 = IsAllow_MCFORMATS ? alGetEnumValue("AL_FORMAT_51CHN16") : -1;
				MULTI_71CH_N8 = IsAllow_MCFORMATS ? alGetEnumValue("AL_FORMAT_71CHN8") : -1;
				MULTI_71CH_N16 = IsAllow_MCFORMATS ? alGetEnumValue("AL_FORMAT_71CHN16") : -1;

				MULTI_51CH_N32 = (IsAllow_MCFORMATS && IsAllow_FLOAT32) ? alGetEnumValue("AL_FORMAT_51CHN32") : -1;
				MULTI_71CH_N32 = (IsAllow_MCFORMATS && IsAllow_FLOAT32) ? alGetEnumValue("AL_FORMAT_71CHN32") : -1;
			}

		}
	}
}
