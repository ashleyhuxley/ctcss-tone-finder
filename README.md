# CTCSS Tone Finder

## Introduction
This is a simple C# program to find which CTCSS tone is being transmitted on a given frequency using Software Defined Radio.

## What is a CTCSS Tone?
CTCSS stands for Continuous Tone-Coded Squelch System. Sometimes referred to as Tone Squelch, it is a feature used to mute users of a two way radio channel who are not using the same tone. This can reduce unwanted interference in shared channels, such as public (PMR) frequencies.

## How does it work?
The program uses rtl_fm to decode audo from an SDR receiver. The audio data is sampled for a short period, after which time the Goertzel algorithm is applied to the data for each of the common CTCSS tones. The program then displays a histogram of the output power of each tone.

## How to use
The program takes a single command line argument - either a PMR channel (P1 to P16) or a raw frequency in MHz.