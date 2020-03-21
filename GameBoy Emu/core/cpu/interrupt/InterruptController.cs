﻿namespace ChichoGB.Core.CPU.Interrupts
{
    public class InterruptController
    {
        public const byte VBLANK_FLAG = 0x1;
        public const byte LCDC_FLAG = 0x2;
        public const byte TIMER_FLAG = 0x4;
        public const byte SERIAL_FLAG = 0x8;
        public const byte JOYPAD_FLAG = 0x10;

        public bool IME { get; set; }

        private Interrupt _vblank;
        private Interrupt _lcdc;
        private Interrupt _timer;
        private Interrupt _serial;
        private Interrupt _joypad;


        public InterruptController()
        {
            IME = false;
            _vblank = new Interrupt(0x40, VBLANK_FLAG);
            _lcdc = new Interrupt(0x48, LCDC_FLAG);
            _timer = new Interrupt(0x50, TIMER_FLAG);
            _serial = new Interrupt(0x58, SERIAL_FLAG);
            _joypad = new Interrupt(0x60, JOYPAD_FLAG);
        }

        public bool isVBlankSet(byte interruptFlag)
        {
            return BitUtils.isBitSet(interruptFlag, 0);
        }

        public bool isLCDCSet(byte interruptFlag)
        {
            return BitUtils.isBitSet(interruptFlag, 1);
        }

        public bool isTimerOverflowSet(byte interruptFlag)
        {
            return BitUtils.isBitSet(interruptFlag, 2);
        }

        public bool isSerialCompleteSet(byte interruptFlag)
        {
            return BitUtils.isBitSet(interruptFlag, 3);
        }

        public bool isJoypadSet(byte interruptFlag)
        {
            return BitUtils.isBitSet(interruptFlag, 4);
        }

        public Interrupt Process(byte interruptFlag, byte interruptEnable)
        {
           return CheckForInterruptRequests(interruptFlag, interruptEnable);
        }

        private Interrupt CheckForInterruptRequests(byte interruptFlag, byte interruptEnable)
        {
            if (isVBlankSet(interruptFlag) && isVBlankSet(interruptEnable))
            {
                _vblank.Process = true;
                return _vblank;
            }

            if (isLCDCSet(interruptFlag) && isLCDCSet(interruptEnable))
            {
                _lcdc.Process = true;
                return _lcdc;
            }

            if (isTimerOverflowSet(interruptFlag) && isTimerOverflowSet(interruptEnable))
            {
                _timer.Process = true;
                return _timer;
            }

            if (isSerialCompleteSet(interruptFlag) && isSerialCompleteSet(interruptEnable))
            {
                _serial.Process = true;
                return _serial;
            }

            if (isJoypadSet(interruptFlag) && isJoypadSet(interruptEnable))
            {
                _joypad.Process = true;
                return _joypad;
            }

            return null;
        }
    }
}