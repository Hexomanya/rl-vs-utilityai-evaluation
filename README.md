# Evaluierung von Reinforcement Learning gegenüber Utility AI für die Entwicklung von KI-Agenten in rundenbasierten Kampfsystemen

## Zusammenfassung
Diese Arbeit untersucht, ob Reinforcement Learning (RL) für kleine Indie-Entwicklerteams eine praktikable Alternative zu Utility AI (UAI) bei der Entwicklung von KI-Agenten in rundenbasierten Kampfsystemen darstellt. Dazu werden drei RL-Varianten sowie ein UAI-Agent in einem eigens entwickelten Prototyp implementiert und anhand von Kampfverhalten, Laufzeitverhalten und Entwicklungskomplexität verglichen. Die Ergebnisse zeigen, dass RL-Agenten zwar höhere Gewinnraten erzielen, UAI jedoch ohne den erheblichen Trainingsaufwand qualitativ gleichwertige Ergebnisse liefert und daher für kleine Indie-Teams empfohlen wird. Darüber hinaus zeigen die Ergebnisse ein Potenzial für den Einsatz von RL als automatisiertes Playtesting-Werkzeug zur frühzeitigen Aufdeckung von Schwachstellen im Game Design.

## Agenten-Mapping
Die folgende Tabelle enthält die Zuordnung der in der Arbeit evaluierten Reinforcement-Learning-Agenten zu den entsprechenden Assets und Konfigurationsdateien innerhalb des Unity-Projekts:

| Agent Name | Asset Name | Konfigurationsdatei |
| :--- | :--- | :--- |
| **PSA** (Position Selector Agent) | `pod_final_auto_06_01` | `conf_ppo_pod_final_06.yaml` |
| **GA-R** (Guided Agent - Random) | `sk_final_auto_01_01` | `conf_ppo_sk_final_auto_01.yaml` |
| **GA-S** (Guided Agent - Self-Play) | `sk_final_advers_01_02` | `conf_ppo_sk_final_advers_01.yaml` |

## Anmerkung zu den Trainingsergebnissen
Die rohen Trainingsergebnisse sowie die vollständigen TensorBoard-Logs sind aufgrund ihres erheblichen Datenvolumens von circa 109 GB nicht in diesem Repository inkludiert. Diese Daten können bei berechtigtem Interesse separat angefragt werden.