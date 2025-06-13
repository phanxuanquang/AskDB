
## **Abstract**

Interacting effectively with relational databases presents significant challenges for users with varying expertise, from formulating complex analytical queries to performing intricate administrative tasks. Current solutions often address either natural language querying or specific automation for database administration (DBA) tasks, lacking a unified, intelligent interface. This paper introduces **AskDB**, a novel LLM-powered agent designed to bridge this gap by providing comprehensive, autonomous support for diverse SQL database interactions, including both data analysis and simplified database administration through natural language. AskDB leverages **Gemini 2.0 Flash** large language model and incorporates key innovations: a **dynamic schema-aware prompting mechanism** (potentially enhanced by semantic search for schema elements) that intelligently utilizes database metadata, and a **task decomposition framework**. This framework enables AskDB to handle complex administrative operations by intelligently planning and executing multi-step actions, which includes capabilities like **auto-debugging generated SQL and autonomously performing real-time internet searches** to gather necessary information, research syntax, refine solutions, and even more. We demonstrate the efficacy of AskDB through extensive experiments on `Specific Text-to-SQL Benchmark` for data interaction, and `Specific Set of DBA Scenarios/Tasks` for administrative functions. Results show that AskDB achieves `Key Quantitative Result for Text-to-SQL` and `Key Quantitative Result for Admin Tasks`. 

The primary contributions of this work include: 
1. AskDB, a unified agent architecture effectively integrating natural language data analysis and relational database administration capabilities.
2. Novel methodologies for schema-aware prompting and an orchestrated, resourceful agentic framework for database interaction, incorporating features like auto-debugging and internet-augmented problem-solving.
3. A comprehensive evaluation demonstrating AskDB's proficiency across both complex analytical query generation and practical database administration scenarios. This research paves the way for more intuitive, efficient, and democratized access to, and management of, relational database systems.

> ##### `Specific Text-to-SQL Benchmark`
> *Example: "the Spider benchmark and a custom dataset of complex analytical queries tailored for financial data analysis (FinanceBench)" or "the BIRD benchmark focusing on execution with diverse SQL constructs."*
> 
> ##### `Specific Set of DBA Scenarios/Tasks`
> *Example: "a curated suite of 20 common PostgreSQL administration tasks covering areas like performance diagnostics (e.g., index suggestion, slow query analysis), user and security management (e.g., role creation, permission auditing), and backup/recovery procedure generation" or "simulated database failure scenarios to evaluate recovery assistance."*
> 
> ##### `Key Quantitative Result for Text-to-SQL`       
> *Example: "85% execution accuracy on complex queries within FinanceBench, outperforming the GPT-4 baseline by 10 percentage points" or "a 90% exact match accuracy on the Spider development set."*
> 
> ##### `Key Quantitative Result for Admin Tasks`
> *Example: "successfully automates 90% of the defined administrative tasks with 95% correctness in generated scripts/commands, reducing estimated manual effort by an average of 30 minutes per task compared to manual execution" or "identified critical security misconfigurations in 5 out of 5 test scenarios."*