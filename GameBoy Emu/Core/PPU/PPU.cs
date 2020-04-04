﻿using ChichoGB.Core.CPU.Interrupts;
using GameBoy_Emu.core.ppu;
using GameBoy_Emu.core.ppu.oam;

namespace ChichoGB.Core
{
    public class Ppu
    {
        private readonly Mmu _ram;
        private readonly BgTileMapManager _bgTileMapManager;
        private readonly PixelFifo _pixelFifo;
        private readonly Fetcher _fetcher;
        public LcdScreen LcdScreen { get; set; }
        public PpuStatus Status { get; set; }

        public int OAM_SEARCH_CYCLES = 80;
        public int PIXEL_PROCESS_CYCLES = 172;
        public int HBLANK_CYCLES = 204;
        public int VBLANK_CYCLES = 456;

        public enum PpuStatus { HBLANK, VBLANK, OAM_SEARCH, PIXEL_TRANSFER };

        public Ppu(Mmu ram, LcdScreen screen)
        {
            _ram = ram;
            _bgTileMapManager = new BgTileMapManager(ram);
            _pixelFifo = new PixelFifo();
            _fetcher = new Fetcher(_bgTileMapManager, ram);
            LcdScreen = screen;
            Status = PpuStatus.OAM_SEARCH;
        }

        int clocks;
        int totalClockPerFrame;
        public void Tick(int cpuCycles)
        {
            totalClockPerFrame += cpuCycles;
            clocks += cpuCycles;
            if (Status == PpuStatus.OAM_SEARCH)
            {
                OamSearch();
            }
            else if (Status == PpuStatus.PIXEL_TRANSFER)
            {
                PixelTransfer(cpuCycles);
            }
            else if (Status == PpuStatus.HBLANK)
            {
                Hblank();
            }
            else if (Status == PpuStatus.VBLANK)
            {
                Vblank();
            }
        }

        private void OamSearch()
        {
            if (clocks >= OAM_SEARCH_CYCLES)
            {
                clocks -= OAM_SEARCH_CYCLES;
                Status = PpuStatus.PIXEL_TRANSFER;

                byte lcdc = _ram.LoadLcdc();
                int spriteHeight = 8;
                if (BitUtils.GetBit(lcdc, 2) == 1)
                {
                    spriteHeight = 16;
                }
                _bgTileMapManager.FindVisibleSprites(LcdScreen.Y, spriteHeight);
            }
        }

        private void PixelTransfer(int cpuCycles)
        {
            while (cpuCycles > 0)
            {
                int work = _pixelFifo.Process(LcdScreen);
                _fetcher.Process(LcdScreen);
                cpuCycles -= work;

                if (_fetcher.State == Fetcher.FetcherState.TRANSFER_READY && _pixelFifo.State == PixelFifo.PixelFifoState.IDLE)
                {
                    _pixelFifo.LoadFifo(_fetcher);
                }
            }

            if (clocks >= PIXEL_PROCESS_CYCLES)
            {
                _pixelFifo.Reset();
                _fetcher.Reset();
                clocks -= PIXEL_PROCESS_CYCLES;
                Status = PpuStatus.HBLANK;
            }
        }

        private void Hblank()
        {
            if (clocks >= HBLANK_CYCLES)
            {
                byte stat = _ram.LoadStat();
                if (!BitUtils.isBitSet(stat, 3))
                {
                    SetHblankInterrupt(stat, 0x8);
                }

                clocks -= HBLANK_CYCLES;
                IncrementLy();
                LcdScreen.X = 0;
                if (LcdScreen.Y == (LcdScreen.Height - 1))
                {
                    Status = PpuStatus.VBLANK;
                }
                else
                {
                    Status = PpuStatus.OAM_SEARCH;
                }
            }
        }

        private void Vblank()
        {
            if (clocks >= VBLANK_CYCLES)
            {
                byte stat = _ram.LoadStat();
                if (!BitUtils.isBitSet(stat, 4))
                {
                    SetVblankInterrupt(stat, 0x11);
                }

                IncrementLy();
                clocks -= VBLANK_CYCLES;

                if (LcdScreen.Y > (LcdScreen.Height + 9))
                {
                    LcdScreen.Draw = true;
                    LcdScreen.Y = 0;
                    Status = PpuStatus.OAM_SEARCH;
                }
            }
        }

        private void IncrementLy()
        {
            _ram.StoreUnsigned8(Mmu.LY_ADDRESS, (byte)LcdScreen.Y++);
        }

        private void SetVblankInterrupt(byte stat, byte flag)
        {
            byte interruptFlag = _ram.Memory[Mmu.IF_ADDRESS];
            interruptFlag = BitUtils.SetBit(interruptFlag, InterruptController.VBLANK_FLAG);
            _ram.StoreUnsigned8(Mmu.IF_ADDRESS, interruptFlag);
            _ram.StoreUnsigned8(Mmu.STAT_ADDRESS, BitUtils.SetBit(stat, flag));
        }

        private void SetHblankInterrupt(byte stat, byte flag)
        {
            byte interruptFlag = _ram.Memory[Mmu.IF_ADDRESS];
            interruptFlag = BitUtils.SetBit(interruptFlag, InterruptController.LCDC_FLAG);
            _ram.StoreUnsigned8(Mmu.IF_ADDRESS, interruptFlag);
            _ram.StoreUnsigned8(Mmu.STAT_ADDRESS, BitUtils.SetBit(stat, flag));
        }
    }
}
