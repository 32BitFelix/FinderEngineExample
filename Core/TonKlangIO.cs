


using NVorbis;
using OpenTK.Audio.OpenAL;

namespace Core.TonKlangIO;

	// A helper class for
    // reading audio files
	public unsafe static class AudioReader
	{
		
        // Reads the data from
        // the given audio file
		public static int ReadData(char* path)
		{
            // Get the managed equivalent
            string mPath = new string(path);

            // The string to hold
            // the magic number
			string magicN;


            // Read the magic number
            // audio file through the
            // binaryreader
			using(BinaryReader b = new BinaryReader(File.Open("./rsc/" + mPath, FileMode.Open)))
			{
				magicN = new string(b.ReadChars(4));
			}


            // If the file appears
            // to be an ogg file...
			if(magicN == "OggS")
                // Read it with the
                // ogg reader
				return ReadOgg("./rsc/" + mPath);


            // If the file appears
            // to be a wav file...
            if(magicN == "WAVE")
                // Read it with the
                // wave reader
			    return ReadWav("./rsc/" + mPath);


            // If the file appears
            // to be a riff file...
			if(!(magicN == "ID30"))
                // Not supported.
                // return 0
				return 0;

            
            // The file cannot be read
            // with the current set of means.
            // Return 0
            return 0;
		}

		// Reads the ogg files
		private unsafe static int ReadOgg(string p)
		{
            // Open a vorbis stream, that
            // reads the given file
			using(VorbisReader vorbis = new VorbisReader(p))
			{
                // The amount of channels
                // from the audio file
				int channels = vorbis.Channels;

                // The way how openal
                // is supposed to read
                // the following buffer
				ALFormat format;

                // If there is more
                // than one channel...
				if(channels != 1)
                    // Get the stereo bitrate
					format = (vorbis.NominalBitrate == 8) ? ALFormat.Stereo8 : ALFormat.Stereo16;
                // If there is only
                // one channel...
				else
                    // Get the mono birate
					format = (vorbis.NominalBitrate == 8) ? ALFormat.Mono8 : ALFormat.Mono16;

                // The samplerate of the
                // audio file
				int sampleRate = vorbis.SampleRate;

                // Allocate an array to hold
				// each partition
				float[] readBuffer = new float[channels * sampleRate / 5];

				List<byte> result = new List<byte>();

				int cnt;

				while((cnt = vorbis.ReadSamples(readBuffer, 0, readBuffer.Length)) > 0)
				{
					for(int i = 0; i < cnt; i++)
					{
						short temp = (short)(32767f * readBuffer[i]);
						result.Add((byte)temp);
						result.Add((byte)(temp >> 8));
					}
				}

				// ^ Get a load of this


                // Generate an openal buffer
				int b = AL.GenBuffer();

				byte[] final = result.ToArray();

                // Get a pointer reference
                // to the array that holds
                // the samples
                fixed(byte* bPtr = final)
                    // Copy the data from there
                    // to the openal buffer
                    AL.BufferData(b, format, bPtr, result.Count, sampleRate);

                // return the buffer
                return b;
			}
		}

		// Reads the wav files
		private unsafe static int ReadWav(string p)
		{

            using (BinaryReader reader = new BinaryReader(File.Open(p, FileMode.Open)))
            {
                // RIFF header
                reader.ReadChars(4);

                int riff_chunck_size = reader.ReadInt32();

                reader.ReadChars(4);

				reader.ReadChars(4);

                int format_chunk_size = reader.ReadInt32();
                int audio_format = reader.ReadInt16();

                int num_channels = reader.ReadInt16();
                int sample_rate = reader.ReadInt32();

                int byte_rate = reader.ReadInt32();
                int block_align = reader.ReadInt16();
                int bits_per_sample = reader.ReadInt16();

				// Initialise the
				// cache for the
				// format of the data
				ALFormat format = 0;

				// If the number of
				// channels isn't one...
				if(num_channels != 1)
					// Get the Stereo format
					format = bits_per_sample != 8 ? ALFormat.Stereo16 : ALFormat.Stereo8;

				else
					// Get the Mono format
					format = bits_per_sample != 8 ? ALFormat.Mono16 : ALFormat.Mono8;


                string data_signature = new string(reader.ReadChars(4));
                if (data_signature != "data")
                    throw new NotSupportedException("Specified wave file is not supported.");

                int data_chunk_size = reader.ReadInt32();


				byte[] result = reader.ReadBytes((int)reader.BaseStream.Length);


                // Generate an openal buffer
				int b = AL.GenBuffer();

                // Get a pointer reference
                // to the array that holds
                // the samples
                fixed(byte* bPtr = result)
                    // Copy the data from there
                    // to the openal buffer
                    AL.BufferData(b, format, bPtr, result.Length, sample_rate);

                // return the buffer
                return b;	
            }
		}
	}
