# Review prompt for any reviewer agent

Paste the block below into any code-review agent (Codex, Gemini, Claude, Copilot, a human) after replacing
`<PR>`. It gives every reviewer the same job, so their answers can be compared. The reviewer must look at the
code, not at other reviewers' threads or the author's replies; independence is the point.

```
You are reviewing pull request <PR> in the GitHub repository noamweisss/internet_speed_test_extension.
Review the diff between the base branch (main) and the PR's head. Read docs/SAFETY-CONTRACT.md and
docs/SECURITY.md first. Do not read other reviewers' comments or the author's replies before forming your own
findings. Do not change any code.

Part 1. Answer these five questions with Yes/No and a file:line for every Yes:
1. Can the extension reach any host other than speed.cloudflare.com?
2. Does it read, write, or delete files, registry keys, or processes?
3. Does it store, log, or send anything about the user (IP, ISP, location, results)? (ADR-0007 accepts the
   metadata every HTTPS request carries; anything beyond that is a Yes.)
4. Does it add a dependency, a Windows capability, or weaken a guard (.githooks, .claude, scripts, workflows)?
5. Is any network input used without bounds on size, time, or format?
A Yes without a linked ADR in docs/decisions/ is a Blocker.

Part 2. List correctness, stability, and security findings. For each: severity (Blocker, Major, Minor, Nit),
file:line, what goes wrong and under which input, and the smallest fix. Verify every finding against the actual
code before reporting it. Style remarks are Nits.

Part 3. One-line verdict: "No blocking findings" or "Blocking findings: <count>".

Post the result as a review on the pull request (with the gh CLI: gh pr review <PR> --comment --body-file
<file>), or return it as text for a human to post. Do not approve or request changes on behalf of any other
reviewer.
```

## Reading the results (owner)

- Two different models answering the five questions the same way is the signal to look for. Their agreement
  means "no known problem", not "correct".
- Hand Blockers and Majors to the building agent: "address the review by <reviewer> on PR <PR>". Nits are optional.
- A review's verdict (Comment, Approve, Request changes) belongs to the reviewer who posted it. Another agent
  cannot clear CodeRabbit's "changes requested"; only CodeRabbit (on re-review) or you (Dismiss review on the PR
  page) can. Resolved threads and a green CI are what tell you the work is done.
