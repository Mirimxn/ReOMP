# Set up a ReOMP modding workspace

1. Use the current workspace root as the checkout location. If it already contains a ReOMP checkout, skip to step 3. If it contains other files, ask the user to open an empty workspace.
2. Get the clone URL from the ReOMP repository that hosts this guide. If the guide was shared on its own, ask for the repository URL or a local Git repository path. Run this command from the empty workspace root.

   ```sh
   git clone -- "<reomp-repository-url-or-local-path>" .
   ```

3. Read [AGENTS.md](AGENTS.md) and load the `omp-mod` skill from [the shared skills directory](.agents/skills/omp-mod/SKILL.md) or [Claude Code's skills directory](.claude/skills/omp-mod/SKILL.md).
4. Report the workspace path. Use the skill to work from the user's mod brief, or ask what the mod should do if they have yet to provide one.
