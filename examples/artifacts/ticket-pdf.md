---
id: T-105
type: ticket
title: Generare PDF factură
epic: E-1
rules:
  - R-1
acceptance:
  - Given client valid, When request invoice pdf, Then returnează fișier PDF.
  - Given sold negativ, When request invoice pdf, Then răspunsul este respins.
---
Implementare endpoint pentru export PDF.

