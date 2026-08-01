#include "doomgeneric.h"
#include <stdio.h>
#include <string.h>
#include <time.h>

#define COLS 120
#define ROWS 45
static const char ramp[] = " .:-=+*#%@";

void DG_Init(void) { printf("\033[2J"); }

void DG_DrawFrame(void)
{
    printf("\033[H"); // cursor home, redraw in place — no scrollback spam
    for (int y = 0; y < ROWS; y++)
    {
        for (int x = 0; x < COLS; x++)
        {
            int sx = x * DOOMGENERIC_RESX / COLS;
            int sy = y * DOOMGENERIC_RESY / ROWS;
            uint32_t px = DG_ScreenBuffer[sy * DOOMGENERIC_RESX + sx];
            int brightness = ((px >> 16 & 0xFF) + (px >> 8 & 0xFF) + (px & 0xFF)) / 3;
            putchar(ramp[brightness * (sizeof(ramp) - 2) / 255]);
        }
        putchar('\n');
    }
}

void DG_SleepMs(uint32_t ms) { struct timespec t = {0, ms * 1000000}; nanosleep(&t, NULL); }
uint32_t DG_GetTicksMs(void) { struct timespec t; clock_gettime(CLOCK_MONOTONIC, &t); return t.tv_sec * 1000 + t.tv_nsec / 1000000; }
int DG_GetKey(int* pressed, unsigned char* key) { return 0; } // no input — the AI's "strategy" is vibes
void DG_SetWindowTitle(const char* title) {}