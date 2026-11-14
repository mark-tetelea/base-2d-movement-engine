# Makes the kit's placeholder sounds: short beeps and noise bursts, synthesised here
# (no recorded or copyrighted audio). Writes 16-bit mono WAVs to Assets/Audio/Placeholder.
# Run from the repo root:  python Tools/sfx/make-sounds.py
# A game replaces them by putting its own clips in the FeelSet's slots.
import math, os, random, struct, wave

RATE = 22050
OUT = os.path.join("Assets", "Audio", "Placeholder")
rng = random.Random(12)


def env(i, n, attack=0.005, curve=3.0):
    t = i / RATE
    a = min(1.0, t / attack) if attack > 0 else 1.0
    return a * (1.0 - i / n) ** curve


def square(phase):
    return 1.0 if (phase % 1.0) < 0.5 else -1.0


def tone(seconds, f0, f1, wave_fn=square, vol=0.5, curve=2.0):
    n = int(seconds * RATE)
    out, phase = [], 0.0
    for i in range(n):
        f = f0 + (f1 - f0) * i / n
        phase += f / RATE
        out.append(wave_fn(phase) * env(i, n, curve=curve) * vol)
    return out


def noise(seconds, vol=0.5, smooth=0.0, curve=2.0):
    # smooth 0 = hiss, near 1 = rumble (a one-pole low-pass).
    n = int(seconds * RATE)
    out, last = [], 0.0
    for i in range(n):
        x = rng.uniform(-1.0, 1.0)
        last = last * smooth + x * (1.0 - smooth)
        out.append(last * env(i, n, curve=curve) * vol / max(0.2, 1.0 - smooth))
    return out


def mix(*parts):
    n = max(len(p) for p in parts)
    return [sum(p[i] for p in parts if i < len(p)) for i in range(n)]


def then(*parts):
    out = []
    for p in parts:
        out += p
    return out


def save(name, samples):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".wav")
    tmp = path + ".tmp"
    with wave.open(tmp, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1.0, min(1.0, s)) * 32000)) for s in samples))
    os.replace(tmp, path)
    print(name, len(samples), "samples")


tri = lambda p: 4.0 * abs((p % 1.0) - 0.5) - 1.0
sine = lambda p: math.sin(2 * math.pi * p)

save("step", noise(0.04, vol=0.25, smooth=0.7, curve=4))
save("shot", mix(tone(0.09, 900, 300, vol=0.3), noise(0.05, vol=0.2)))
save("swing", noise(0.14, vol=0.35, smooth=0.5, curve=1.5))
save("dash", mix(noise(0.16, vol=0.3, smooth=0.3, curve=1.2), tone(0.16, 200, 500, tri, vol=0.15)))
save("hit", mix(tone(0.07, 300, 120, vol=0.35), noise(0.06, vol=0.3, smooth=0.3)))
save("melee-hit", mix(tone(0.1, 160, 60, tri, vol=0.6), noise(0.08, vol=0.35, smooth=0.6)))
save("hurt", tone(0.22, 520, 180, vol=0.35, curve=1.2))
save("death", mix(noise(0.35, vol=0.45, smooth=0.75, curve=1.5), tone(0.3, 220, 50, vol=0.25, curve=1.5)))
save("break", then(noise(0.05, vol=0.5, smooth=0.4), noise(0.12, vol=0.35, smooth=0.6)))
save("pickup", then(tone(0.06, 880, 880, vol=0.25, curve=0.5), tone(0.09, 1320, 1320, vol=0.25)))
save("door", mix(tone(0.16, 110, 70, tri, vol=0.6, curve=1.5), noise(0.1, vol=0.2, smooth=0.8)))
save("crumble", noise(0.3, vol=0.5, smooth=0.85, curve=1.2))
save("click", tone(0.025, 1200, 1000, sine, vol=0.3, curve=3))
