# © 2024 Phan Xuan Quang / AskDB

[![SplashScreen scale-200](https://i.imgur.com/kO68bFg.png)](https://apps.microsoft.com/detail/9p1mcx47432z)

*   What if you're a person who need to do ad-hoc data queries but are not technical enough to write SQL queries?
*   What if you could simply ask your database questions in plain language and get instant answers?
*   What if you could have something that learns from your data and helps you find patterns, trends, and insights without needing to be a data expert?
*   What if you even don't know what to do with your raw data?
  
Introducing **AskDB**, an AI agent that lets you interact with your databases in a natural, intuitive way. You can ask questions, get insights, and even let AskDB handle complex data-related tasks without needing deep knowledge about SQL or database management.

![image](https://github.com/user-attachments/assets/49295d11-d439-4e63-b24f-e2f8014452bc)

AskDB is designed to be your go-to tool for interacting with databases, whether you're a business analyst, data scientist, manager, or just someone who needs to work with data. It makes data tasks easier and safer, so you can focus on what matters most.

For further details, please refer to the showcasing video below!

[![Showcase Video](https://github.com/user-attachments/assets/3c09cd09-0c95-47a4-8082-875d51971856)](https://www.youtube.com/watch?v=GeAdZXIc2y0)


## **Who benefits from AskDB**
*   **Business Analysts & Strategists:** Get answers faster. Ask complex questions and get insights without needing to be a database expert. This means quicker, smarter decisions.
*   **Data Scientists:** Save time on routine tasks. Let AskDB handle initial data checks and basic troubleshooting, so you can focus on advanced analysis and modeling.
*   **Managers & Decision Makers:** Understand your business better. Get clear reports and summaries in plain language, helping you act quickly on what your data is telling you.
*   **Developers:** Work more efficiently. Quickly check data or explore databases with an AI helper that also keeps things safe.
*   **Database Administrators:** Empower your users safely. Give people a smart tool they can use themselves, reducing your ad-hoc request load while maintaining data security.
*   **Marketing & Sales Teams:** Understand customers easily. Ask about sales trends or campaign results and get direct insights to help refine your strategies.
*   **Operations & Logistics Staff:** Spot issues and opportunities. Analyze operational data quickly to find inefficiencies or areas for improvement.
*   **Students & Learners:** Learn by doing, safely. Explore data and see how an AI approaches analysis, even explaining how it solves problems.

## **Supported Databases**

AskDB supports 5 popular database providers as listed below, so you can work with the systems you already use. It connects to your databases securely and helps you get the most out of your data without needing to be a database expert.

*   Microsoft SQL Server
*   MySQL
*   PostgreSQL
*   SQLite
*   MariaDB

No matter how complex your database is and how many tables it has, AskDB can help you understand and manage it easily.

## **What can AskDB do**
![image](https://github.com/user-attachments/assets/ae10a017-1986-4c4c-a72c-f084e6d38c9c)

> _In this section, the `AdventureWorksDW2019` database is used for demonstration. AdventureWorks is a large-scale sample data warehouse provided by Microsoft, designed for simulating real-world business analytics scenarios, it can be found and downloaded [**HERE**](https://learn.microsoft.com/en-us/sql/samples/adventureworks-install-configure)_

AskDB can understands your questions and gives you clear answers, even if you're not a database expert. No need for complex code for many tasks, just tell AskDB what you need in everyday words.

In addition, AskDB provides you with dynamic, context-aware suggestions for follow-up questions, data filters, and analytical approaches, tailoring the interaction to your evolving needs and the Agent's own discoveries during its autonomous operation.

Just provide AskDB with a data-related objective or task, and it will independently devise and execute a multi-step plan to achieve it. This includes understanding complex requests, clarifying your requirements, breaking them down, and managing the workflow through to completion.

Faced with operational errors, query failures, or unexpected data, AskDB doesn't simply stop but initiating a diagnostic process, hypothesizes causes, develops a corrective action plan, and attempts to self-correct to fulfill your requests.

![image](https://github.com/user-attachments/assets/a263d401-9e1b-4418-aa6f-da7b4f1261ae)

AskDB doesn't just give you data; it helps you see what's important, and suggest actionable insights.
 
AskDB can autonomously investigate your database to identify key tables and relationships. It then moves beyond mere data presentation to actively analyze results, identify significant patterns, and offer explanations, often proposing further analytical paths. 

AskDB can also access Google Search engine to research for up-to-date information, best practices, debugging tips, error resolutions, or other relevant information that can help you continue on the current task more accurately and effectively.

![Video_2025-06-08_220401 mp4_snapshot_02 47 804](https://github.com/user-attachments/assets/27070960-6682-4acc-87d4-a75110ccb602)

Safety and privacy is AskDB's top priority. Any action that changes data needs your clear "yes" first, after AskDB explains its plan. 

Advanced safety protocols, including a **Confirmation Gauntlet** for critical operations, are seamlessly woven into the conversational flow. This ensures data integrity while allowing AskDB to operate with a high degree of autonomy for non-destructive tasks.

![image](https://github.com/user-attachments/assets/8d389040-aae6-4a14-a1c0-adfb32dab2fc)

---

## :blue_book: Installation
### Official Version
- [Microsoft Store](https://apps.microsoft.com/store/detail/9P1MCX47432Z?cid=DevShareMCLPCS)

### Portable Version
> *The portable version is a alternative for the official version in case you cannot install AskDB from Microsoft Store.*

- Step 1: Download and extract the **AskDB.zip** from [**THIS**](https://github.com/phanxuanquang/AskDB/releases/latest) release.
- Step 2: Run the **AskDB.exe** file (skip the **Unknown Published** warning from Windows if any).
- Step 3: Follow the instruction to get started.
- Step 4: Ask AskDB everything about your SQL database.

---

## :lock: Data Privacy
Your data's security is the top priority:
*   **You control your credentials.** All credentials are stored securely in encrypted files on your local machine while ensure no other third-party services can decrypt them.
*   **Clear and controlled actions.** You're always informed, and critical operations need your approval.

Currently, AskDB utilizes the Google Gemini as the core AI engine. The user should read and understand the [Gemini API Additional Terms of Service](https://ai.google.dev/gemini-api/terms) before using AskDB.

---

## :open_hands: Contribution
I welcome contributions and encourage you to help this project better and better. If you encounter any issues or have suggestions for improvements, please open an issue in the [Issues](https://github.com/phanxuanquang/AskDB/issues) section of the repository.
Before submitting a pull request, please ensure that your changes are well-documented in the Pull Request description.

Thank you for your contribution and for helping to make this project better! :tada:

## 📄 License
This project is protected by a **Strict Non-Commercial License**.  

*   Forks and contributions are allowed.  
*   Commercial use is strictly forbidden under any circumstances.  
*   Clear attribution and a link to the original repo are mandatory for any redistribution.
  
For full terms, see [LICENSE](./LICENSE).

---

## Future Improvements
*   Ultilize semantic searching instead of fuzzy searching for the table names searching functioon.
*   Support local models → Phi-4 family
*   The features for application setting and connection credential management.

