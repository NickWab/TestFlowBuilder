namespace ATECore.TestFlowBuilder.Models;

public static class TestLibrary
{
    public static IReadOnlyList<TestDefinition> All { get; } =
    [
        new("psu_rail", "Power Supply", "Instrument.Hal.PowerSupply", "Set a rail, settle, verify voltage and current draw.",
            """
            // Rail bring-up on CH1
            var psu = Hal.Open<IPowerSupply>("PSU-3021", channel: 1);

            psu.SetVoltage(3.300);
            psu.SetCurrentLimit(0.500);
            psu.OutputEnable(true);
            Delay.Ms(250);            // settle

            var v = psu.MeasureVoltage();
            var i = psu.MeasureCurrent();
            Log.Info($"CH1 = {v:F4} V @ {i*1000:F1} mA");

            Assert.InRange(v, 3.267, 3.333, "rail tolerance +/-1%");
            Assert.Less(i, 0.400, "quiescent current");
            psu.OutputEnable(false);
            """,
            [new("CH1 voltage", "3.3021 V", "3.267 – 3.333"), new("CH1 current", "212.4 mA", "< 400"), new("Settling time", "138 ms", "< 250")],
            [new("PASS", "InRange(v, 3.267, 3.333) — rail tolerance +/-1%"), new("PASS", "Less(i, 0.400) — quiescent current")],
            "[00.000] open PSU-3021 @ USB0::0x0957\n[00.004] SetVoltage 3.300 V\n[00.006] SetCurrentLimit 0.500 A\n[00.008] OutputEnable true\n[00.146] CH1 = 3.3021 V @ 212.4 mA\n[00.148] OutputEnable false\n[00.149] 2 assertions, 0 failed",
            0.62),

        new("adc", "ADC Linearity", "Instrument.Hal.Adc", "Sweep the reference and check code error across channels.",
            """
            // 16-bit ADC sweep on CH0
            var adc = Hal.Open<IAdc>("ADS8681");
            var src = Hal.Open<ISourceMeter>("SMU-2450");

            adc.Configure(bits: 16, refVolts: 4.096, rate: 100_000);

            foreach (var target in new[] { 0.5, 1.0, 2.0, 3.0, 4.0 })
            {
                src.SetVoltage(target);
                Delay.Ms(20);
                var code = adc.ReadRaw(channel: 0);
                var volts = code * 4.096 / 65536.0;
                Log.Info($"{target:F1} V -> code {code} ({volts:F4} V)");
                Assert.Near(volts, target, 0.002, $"linearity @ {target} V");
            }
            """,
            [new("INL (max)", "0.8 LSB", "< 2.0"), new("DNL (max)", "0.4 LSB", "< 1.0"), new("Offset error", "1.2 mV", "< 5.0"), new("Gain error", "0.03 %", "< 0.10")],
            [new("PASS", "Near(volts, target, 0.002) × 5 points"), new("PASS", "INL within datasheet limit")],
            "[00.000] ADS8681 configured 16b / 4.096 V / 100 kSPS\n[00.021] 0.5 V -> code 8003 (0.5002 V)\n[00.043] 1.0 V -> code 16004 (1.0003 V)\n[00.065] 2.0 V -> code 32009 (2.0006 V)\n[00.088] 3.0 V -> code 48011 (3.0007 V)\n[00.110] 4.0 V -> code 64014 (4.0009 V)\n[00.112] 5 assertions, 0 failed",
            1.04),

        new("versions", "Versions", "Instrument.Hal.Device", "Read firmware, bootloader and driver versions; match manifest.",
            """
            // Identity and version manifest
            var dev = Hal.Open<IDevice>("DUT");

            var fw   = dev.ReadVersion(VersionKind.Firmware);
            var boot = dev.ReadVersion(VersionKind.Bootloader);
            var hw   = dev.ReadVersion(VersionKind.Hardware);
            var drv  = Hal.DriverVersion;

            Log.Info($"fw {fw} / boot {boot} / hw {hw} / drv {drv}");

            Assert.Equal(fw,  Manifest.Expected.Firmware);
            Assert.Equal(boot, Manifest.Expected.Bootloader);
            Assert.AtLeast(drv, new Version(4, 2, 0));
            """,
            [new("Firmware", "2.7.4", "= 2.7.4"), new("Bootloader", "1.0.9", "= 1.0.9"), new("Hardware rev", "C2", "B1 | C2"), new("Driver", "4.3.1", ">= 4.2.0")],
            [new("PASS", "Equal(fw, 2.7.4)"), new("PASS", "Equal(boot, 1.0.9)"), new("PASS", "AtLeast(drv, 4.2.0)")],
            "[00.000] DUT enumerated on COM7\n[00.031] fw 2.7.4 / boot 1.0.9 / hw C2 / drv 4.3.1\n[00.033] 3 assertions, 0 failed",
            0.21),

        new("dmm", "DMM Measure", "Instrument.Hal.Dmm", "4-wire resistance and DC voltage against a reference.",
            """
            // 4-wire shunt check
            var dmm = Hal.Open<IDmm>("DMM-34465A");
            dmm.Function = DmmFunction.FourWireResistance;
            dmm.NPLC = 10;

            var r = dmm.Measure();
            Log.Info($"R_shunt = {r*1000:F3} mOhm");
            Assert.Near(r, 0.100, 0.001, "shunt 100 mOhm +/-1%");
            """,
            [new("R shunt", "99.82 mΩ", "99.0 – 101.0"), new("Lead resistance", "0.4 mΩ", "< 5.0")],
            [new("PASS", "Near(r, 0.100, 0.001) — shunt 100 mOhm +/-1%")],
            "[00.000] DMM-34465A · 4W resistance · NPLC 10\n[00.201] R_shunt = 99.820 mOhm\n[00.202] 1 assertion, 0 failed",
            0.44),

        new("i2c", "I2C Bus Scan", "Instrument.Hal.I2c", "Enumerate addresses and confirm the expected device map.",
            """
            // Bus inventory at 400 kHz
            var bus = Hal.Open<II2cMaster>("FT4222", speedHz: 400_000);
            var found = bus.Scan(0x08, 0x77);

            Log.Info("found: " + string.Join(", ", found.Select(a => $"0x{a:X2}")));
            Assert.Contains(found, 0x48, "ADC");
            Assert.Contains(found, 0x50, "EEPROM");
            Assert.Equal(found.Count, 3);
            """,
            [new("Devices found", "3", "= 3"), new("Bus speed", "399.6 kHz", "380 – 420"), new("Rise time", "186 ns", "< 300")],
            [new("PASS", "Contains(found, 0x48) — ADC"), new("PASS", "Contains(found, 0x50) — EEPROM"), new("PASS", "Equal(found.Count, 3)")],
            "[00.000] FT4222 master @ 400 kHz\n[00.058] found: 0x48, 0x50, 0x68\n[00.060] 3 assertions, 0 failed",
            0.33),

        new("temp", "Thermal Soak", "Instrument.Hal.Thermal", "Hold a setpoint and verify sensor agreement while loaded.",
            """
            // 15 s soak at 55 C
            var ch = Hal.Open<IThermalChamber>("TC-500");
            var dut = Hal.Open<IDevice>("DUT");

            ch.SetPoint(55.0);
            ch.WaitStable(toleranceC: 0.5, timeout: TimeSpan.FromMinutes(4));

            var tDut = dut.ReadSensor(SensorId.Die);
            var tRef = ch.ReadProbe();
            Assert.Near(tDut, tRef, 2.0, "die vs chamber probe");
            """,
            [new("Chamber probe", "55.1 °C", "54.5 – 55.5"), new("Die sensor", "56.4 °C", "± 2.0 of probe"), new("Soak time", "15.0 s", ">= 15")],
            [new("PASS", "Near(tDut, tRef, 2.0) — die vs chamber probe")],
            "[00.000] TC-500 setpoint 55.0 C\n[02.140] stable within 0.5 C\n[17.142] die 56.4 C / probe 55.1 C\n[17.143] 1 assertion, 0 failed",
            2.10),

        new("gpio", "Digital I/O Loopback", "Instrument.Hal.Gpio", "Walk a pattern across the header and read it back.",
            """
            // Walking-ones loopback, 16 pins
            var io = Hal.Open<IGpio>("DIO-32");
            io.SetDirection(0x0000FFFF, Direction.Output);

            for (int bit = 0; bit < 16; bit++)
            {
                uint pattern = 1u << bit;
                io.Write(pattern);
                Delay.Us(50);
                uint read = io.Read() >> 16;
                Assert.Equal(read, pattern, $"loopback bit {bit}");
            }
            """,
            [new("Patterns tested", "16", "= 16"), new("Mismatches", "0", "= 0"), new("Propagation", "38 ns", "< 100")],
            [new("PASS", "Equal(read, pattern) × 16 bits")],
            "[00.000] DIO-32 direction mask 0x0000FFFF\n[00.019] walking ones 0x0001 … 0x8000\n[00.020] 16 assertions, 0 failed",
            0.28),

        new("eeprom", "EEPROM Read/Write", "Instrument.Hal.Storage", "Round-trip the calibration page and restore contents.",
            """
            // Non-destructive page round-trip
            var mem = Hal.Open<IEeprom>("24LC256", address: 0x50);
            var backup = mem.Read(page: 4, length: 64);

            var pattern = Bytes.Random(64, seed: 7);
            mem.Write(page: 4, pattern);
            Delay.Ms(6);                   // write cycle
            var back = mem.Read(page: 4, length: 64);

            Assert.SequenceEqual(back, pattern, "page 4 round-trip");
            mem.Write(page: 4, backup);    // restore
            """,
            [new("Bytes verified", "64", "= 64"), new("Write cycle", "4.8 ms", "< 6.0"), new("Bit errors", "0", "= 0")],
            [new("PASS", "SequenceEqual(back, pattern) — page 4 round-trip"), new("PASS", "backup restored")],
            "[00.000] 24LC256 @ 0x50\n[00.012] page 4 backed up (64 B)\n[00.019] wrote 64 B, verified 64 B\n[00.027] page 4 restored\n[00.028] 2 assertions, 0 failed",
            0.37),
    ];

    public static TestDefinition? Find(string? key) => All.FirstOrDefault(t => t.Key == key);
}
