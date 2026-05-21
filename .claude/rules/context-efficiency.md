Create .claude/rules/context-efficiency.md with this content:

# Context Efficiency Rules

When starting any new task or PR:

- Read PROGRESS.md first — always
- Read only files directly relevant to the task
- Do not launch broad Explore agents unless explicitly asked
- Do not use writing-plans skill for small or medium PRs
- If scope is unclear — ask one clarifying question, do not explore
- Prefer reading 3 specific files over scanning 30 files

When asked "next pr" or similar open prompts:
- Read PROGRESS.md to find next planned item
- Ask: "Is this the right next task?" before exploring
- Wait for confirmation before launching agents

Token budget awareness:
- If context is above 40% before implementation starts — warn the user
- Suggest /clear if context is above 60% before a new PR