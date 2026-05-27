# Evaluation of Reinforcement Learning versus Utility AI for the Development of AI Agents in Turn-Based Combat Systems

Language: English | [Deutsch](README.de.md)  
[Read the full Bachelor's Thesis (German)](Thesis/bachelorArbeit_v01.pdf)

---

## Abstract
This thesis investigates whether Reinforcement Learning (RL) represents a viable alternative to Utility AI (UAI) for small indie developer teams when developing AI agents in turn-based combat systems. To this end, three RL variants and one UAI agent were implemented in a custom-developed prototype and compared based on combat behavior, runtime behavior, and development complexity. 

The results show that while RL agents achieve higher win rates, UAI delivers qualitatively equivalent results without the significant training overhead and is therefore recommended for small indie teams. Furthermore, the results indicate a high potential for using RL as an automated playtesting tool to uncover vulnerabilities in game design at an early stage.

## Agent Mapping
The following table maps the Reinforcement Learning agents evaluated in the thesis to their corresponding assets and configuration files within the Unity project:

| Agent Name | Asset Name | Configuration File |
| :--- | :--- | :--- |
| **PSA** (Position Selector Agent) | `pod_final_auto_06_01` | `conf_ppo_pod_final_06.yaml` |
| **GA-R** (Guided Agent - Random) | `sk_final_auto_01_01` | `conf_ppo_sk_final_auto_01.yaml` |
| **GA-S** (Guided Agent - Self-Play) | `sk_final_advers_01_02` | `conf_ppo_sk_final_advers_01.yaml` |

## Note on Training Results
The raw training results as well as the complete TensorBoard logs are not included in this repository due to their significant data volume of approximately 109 GB. This data can be requested separately upon justified interest.

## License
This project is licensed under the MIT License – see the [LICENSE](LICENSE) file for details.

## Contact
Benjamin Schön: bs-192047@rwu.de

Inquiries regarding the project or the detailed training data (109 GB) can be made via the provided email address.