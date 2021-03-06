#include "stdafx.h"

int main()
{
	constexpr unsigned int width = 512;
	constexpr unsigned int height = 512;

	struct DataFormat {
		float r, g;
	};

	DataFormat* data = new DataFormat[width * height];

	for (int y = 0; y < height; ++y)
	{
		for (int x = 0; x < width; ++x)
		{
			data[y * width + x].r = float(x);
			data[y * width + x].g = float(y);
		}
	}

	FILE * writePtr = fopen("test.bin", "wb");  // w for write, b for binary;
	fwrite(data, sizeof(DataFormat) * width * height, 1, writePtr); // write 10 bytes from our buffer
	fclose(writePtr);

	delete[] data;

    return 0;
}

