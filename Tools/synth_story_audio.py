# -*- coding: utf-8 -*-
"""
Generates the original Assignment 3 story audio for Jay A Career (no third-party samples).

Music (D minor, slow, sparse — tension carried by harmony rather than tempo):
  Theme_Intro.wav   ~36 s  Dm  Bb  F  C  (x2)          opening cutscene: "the ordinary world"
  Theme_Ending.wav  ~32 s  Dm  Gm  Bb  A               closing cutscene: ends on the unresolved A (cliffhanger)
  Theme_Menu.wav    ~43 s  Dm9 Bbmaj7 Gm7 Asus4 (loop)  main menu / end screen, seamless loop
Story sound cues (sound design lecture: gameplay cues, feedback, emotion):
  Phone_Buzz.wav    2.4 s loop  Luka's phone vibrating on the counter until the player reads it
  Car_Idle.wav      4.0 s loop  Paul's car idling at the end of the road
  Lab_Hum.wav       2.0 s loop  Hale's ceiling fixtures (silent when the lab is dark on Day 3)
  Footsteps.wav     2.6 s       Hale walking back into the lab

Usage: python synth_story_audio.py <output folder>
"""
import sys, os, wave
import numpy as np
from scipy.signal import lfilter, butter, sosfilt, sosfiltfilt

SR = 44100
rng = np.random.default_rng(909)
NOTE = {n: i for i, n in enumerate(["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"])}


def hz(name):
    """'D4' -> frequency."""
    n, o = name[:-1], int(name[-1])
    return 440.0 * 2 ** ((NOTE[n] + 12 * (o + 1) - 69) / 12)


def env(n, a, r, sustain=1.0):
    e = np.ones(n) * sustain
    ai = min(n, int(a * SR)); ri = min(n - ai, int(r * SR))
    if ai: e[:ai] = np.linspace(0, sustain, ai)
    if ri: e[n - ri:] = np.linspace(sustain, 0, ri) ** 1.6
    return e


def pad_voice(f, dur, detune=0.0):
    """Soft analogue-style pad: band-limited saw with falling harmonic weights."""
    t = np.arange(int(dur * SR)) / SR
    out = np.zeros_like(t)
    for k in range(1, 9):
        out += np.sin(2 * np.pi * f * k * (1 + detune) * t + rng.uniform(0, 6.28)) / (k ** 1.6)
    return out


def pluck(f, dur=2.4, bright=1.0):
    """Felt-piano-like pluck: decaying partials, slightly inharmonic."""
    t = np.arange(int(dur * SR)) / SR
    out = np.zeros_like(t)
    for k, w in enumerate([1, .45 * bright, .22 * bright, .1 * bright], start=1):
        out += w * np.sin(2 * np.pi * f * k * (1 + .0007 * k * k) * t) * np.exp(-t * (1.6 + .9 * k))
    attack = np.minimum(1, t / .006)
    return out * attack


def reverb(x, mix=.32, room=.84):
    """Small Schroeder reverb (4 combs + 2 all-pass) — enough to place sounds in a space."""
    y = np.zeros_like(x)
    for d, g in [(1557, room), (1617, room - .01), (1491, room - .02), (1422, room - .03)]:
        a = np.zeros(d + 1); a[0] = 1; a[d] = -g
        y += lfilter([1], a, x)
    y /= 4
    for d, g in [(225, .5), (556, .5)]:
        b = np.zeros(d + 1); b[0] = -g; b[d] = 1
        a = np.zeros(d + 1); a[0] = 1; a[d] = -g
        y = lfilter(b, a, y)
    return (1 - mix) * x + mix * y


def lowpass(x, cutoff, order=2):
    return sosfilt(butter(order, cutoff, "low", fs=SR, output="sos"), x)


def place(buf, sig, start):
    i = int(start * SR); j = min(len(buf), i + len(sig))
    if i < len(buf): buf[i:j] += sig[: j - i]


def stereo(left, right):
    return np.stack([left, right], axis=1)


def normalise(x, peak=.8):
    m = np.max(np.abs(x)) or 1
    return x / m * peak


def write(path, x):
    x = np.clip(x, -1, 1)
    data = (x * 32767).astype("<i2")
    ch = 1 if data.ndim == 1 else data.shape[1]
    with wave.open(path, "wb") as w:
        w.setnchannels(ch); w.setsampwidth(2); w.setframerate(SR); w.writeframes(data.tobytes())
    print("wrote", os.path.basename(path), f"{len(x) / SR:.1f}s")


# ------------------------------------------------------------------ music
def render_music(chords, chord_len, melody, total, pad_level=.22, bass_level=.30, pluck_level=.34, brightness=1.0):
    n = int(total * SR)
    L = np.zeros(n); R = np.zeros(n)
    for ci, notes in enumerate(chords):
        start = ci * chord_len
        dur = chord_len + 1.2                        # overlap so chords crossfade
        for k, note in enumerate(notes[1:]):         # pad voices
            f = hz(note)
            e = env(int(dur * SR), 1.4, 1.6)
            place(L, pad_voice(f, dur, -.0025) * e * pad_level / len(notes), start)
            place(R, pad_voice(f, dur, +.0025) * e * pad_level / len(notes), start)
        b = hz(notes[0]); t = np.arange(int(dur * SR)) / SR   # bass: root, soft sine + octave
        bass = (np.sin(2 * np.pi * b * t) + .25 * np.sin(4 * np.pi * b * t)) * env(len(t), .5, 1.4) * bass_level
        place(L, bass, start); place(R, bass, start)
    for when, note, vel in melody:                   # sparse melody, panned slightly
        p = pluck(hz(note), bright=brightness) * pluck_level * vel
        place(L, p * .9, when); place(R, p * 1.0, when + .012)
    L = lowpass(L, 5200); R = lowpass(R, 5200)
    return stereo(reverb(L), reverb(R, room=.85))


def theme_intro():
    chords = [["D2", "D3", "A3", "F4", "A4"], ["A#1", "A#2", "F3", "D4", "F4"], ["F2", "F3", "C4", "A4", "C5"], ["C2", "C3", "G3", "E4", "G4"]] * 2
    m = []
    motif = [("A4", 0), ("D5", .9), ("E5", 1.8), ("F5", 2.7), ("E5", 3.6)]
    for bar, shift in enumerate([0, 0, 0, 0, 0, 0, 0, 0]):
        base = bar * 4.5
        if bar in (0, 4):
            m += [(base + o, n, .9) for n, o in motif]
        elif bar in (1, 5):
            m += [(base + .0, "D5", .8), (base + 1.8, "F5", .7), (base + 2.7, "D5", .6)]
        elif bar in (2, 6):
            m += [(base + .0, "C5", .8), (base + .9, "A4", .6), (base + 2.7, "F4", .6)]
        else:
            m += [(base + .0, "E5", .7), (base + 1.8, "G4", .6)] if bar == 3 else [(base, "E4", .7), (base + 2.2, "D4", .55)]
    x = render_music(chords, 4.5, m, 36 + 4)
    fade = env(len(x), .8, 4.0)[:, None]
    return normalise(x * fade, .75)


def theme_ending():
    chords = [["D2", "D3", "A3", "F4"], ["G1", "G2", "D3", "A#3", "D4"], ["A#1", "A#2", "F3", "D4"], ["A1", "A2", "E3", "C#4", "E4"]]
    m = [(0.0, "F5", .8), (1.6, "E5", .6), (3.2, "D5", .7), (6.0, "A#4", .7), (7.8, "D5", .6), (12.0, "F5", .7), (13.5, "A5", .55),
         (18.0, "E5", .8), (20.5, "C#5", .7), (24.0, "A4", .6)]          # ends on C#/A: unresolved dominant
    x = render_music(chords, 6.0, m, 32, pad_level=.25, bass_level=.34, brightness=.8)
    fade = env(len(x), .5, 5.0)[:, None]
    return normalise(x * fade, .75)


def theme_menu():
    chords = [["D2", "D3", "A3", "E4", "F4"], ["A#1", "A#2", "F3", "A3", "D4"], ["G1", "G2", "D3", "F3", "A#3"], ["A1", "A2", "D3", "E3", "A3"]]
    cycle = 4 * 10.8
    m = [(1.0, "A4", .55), (4.5, "F4", .45), (11.8, "D5", .5), (15.0, "A4", .4), (22.6, "A#4", .5), (26.4, "F4", .4), (33.4, "E4", .5), (37.0, "A4", .45)]
    reps = 3                                          # render three cycles, keep the middle one -> seamless loop
    chords3 = chords * reps
    m3 = [(t + r * cycle, n, v) for r in range(reps) for (t, n, v) in m]
    x = render_music(chords3, 10.8, m3, cycle * reps + 2, pad_level=.2, bass_level=.22, pluck_level=.26, brightness=.7)
    a = int(cycle * SR)
    y = x[a:2 * a].copy()
    k = int(1.5 * SR)                                 # crossfade the tail into the audio that precedes the loop start
    w = np.linspace(0, 1, k)[:, None]
    y[-k:] = y[-k:] * (1 - w) + x[a - k:a] * w
    return normalise(y, .6)


# ------------------------------------------------------------------ sound cues
def phone_buzz():
    n = int(2.4 * SR); t = np.arange(n) / SR
    motor = np.sign(np.sin(2 * np.pi * 155 * t)) * .5 + np.sin(2 * np.pi * 310 * t) * .3
    rattle = lowpass(rng.standard_normal(n), 2500) * .35
    gate = np.zeros(n)
    for s0 in (0.0, 0.55):
        i, j = int(s0 * SR), int((s0 + .38) * SR)
        g = np.ones(j - i); r = int(.015 * SR); g[:r] = np.linspace(0, 1, r); g[-r:] = np.linspace(1, 0, r)
        gate[i:j] = g
    x = (motor + rattle * (1 + .5 * np.sin(2 * np.pi * 31 * t))) * gate
    x = sosfilt(butter(2, [120, 3500], "band", fs=SR, output="sos"), x)
    return normalise(x, .7)


def car_idle():
    dur = 4.0; n = int(dur * SR); t = np.arange(n) / SR
    f = 27.5                                         # 110 whole cycles in 4 s -> seamless loop
    x = np.zeros(n)
    for k, w in [(1, 1), (2, .6), (3, .35), (4, .25), (6, .12)]:
        x += w * np.sin(2 * np.pi * f * k * t + k)
    x *= 1 + .18 * np.sin(2 * np.pi * 1.5 * t)       # 6 slow wobbles per loop
    noise = np.fft.irfft(np.fft.rfft(rng.standard_normal(n)) * (np.fft.rfftfreq(n, 1 / SR) < 300), n)  # circular -> loops
    x = x * .7 + normalise(noise, .5)
    return normalise(x, .7)


def lab_hum():
    dur = 2.0; n = int(dur * SR); t = np.arange(n) / SR
    x = .6 * np.sin(2 * np.pi * 100 * t) + .3 * np.sin(2 * np.pi * 200 * t) + .12 * np.sin(2 * np.pi * 300 * t)
    buzz = np.fft.irfft(np.fft.rfft(rng.standard_normal(n)) * ((np.fft.rfftfreq(n, 1 / SR) > 3000) & (np.fft.rfftfreq(n, 1 / SR) < 7000)), n)
    x += normalise(buzz, .05) * (1 + np.sin(2 * np.pi * 100 * t))
    return normalise(x, .5)


def footsteps():
    n = int(2.6 * SR); x = np.zeros(n)
    for i, s0 in enumerate([0.15, 0.72, 1.28, 1.86]):
        m = int(.22 * SR); t = np.arange(m) / SR
        heel = lowpass(rng.standard_normal(m), 1800) * np.exp(-t * 55)
        thump = np.sin(2 * np.pi * 85 * t) * np.exp(-t * 38) * .8
        scuff = sosfilt(butter(2, [900, 5000], "band", fs=SR, output="sos"), rng.standard_normal(m)) * np.exp(-((t - .05) / .03) ** 2) * .25
        place(x, (heel + thump + scuff) * (.75 + .08 * i), s0)
    return normalise(reverb(x, mix=.22, room=.7), .8)


if __name__ == "__main__":
    out = sys.argv[1]
    os.makedirs(out, exist_ok=True)
    write(os.path.join(out, "Theme_Intro.wav"), theme_intro())
    write(os.path.join(out, "Theme_Ending.wav"), theme_ending())
    write(os.path.join(out, "Theme_Menu.wav"), theme_menu())
    write(os.path.join(out, "Phone_Buzz.wav"), phone_buzz())
    write(os.path.join(out, "Car_Idle.wav"), car_idle())
    write(os.path.join(out, "Lab_Hum.wav"), lab_hum())
    write(os.path.join(out, "Footsteps.wav"), footsteps())
