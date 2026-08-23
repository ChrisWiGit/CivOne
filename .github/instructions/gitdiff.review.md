# Copilot Review Prompt – Minimal Git Diffs

## Goal

Review only relevant changes with minimal token usage.

Use short Git diffs for:

- staged changes
- current branch changes vs main/master
- single files

Avoid full file outputs.

---

## Rules

- Always prefer `--stat` or `--unified=1`
- Never print complete files
- Focus only on relevant changes
- Ignore lockfiles, build artifacts, and minified files
- Keep context windows very small
- Start with summaries, then inspect targeted diffs

---

## 1. Review staged changes

### Overview

```bash
git diff --cached --stat
```

### Minimal diff

```bash
git diff --cached --unified=1 --minimal
```

### Changed files only

```bash
git diff --cached --name-only
```

---

## 2. Review branch vs main

### Overview 1

```bash
git diff origin/main...HEAD --stat
```

### Minimal diff 1

```bash
git diff origin/main...HEAD --unified=1 --minimal
```

### Changed files only 1

```bash
git diff origin/main...HEAD --name-only
```

---

## 3. Review a single file

```bash
git diff --cached --unified=1 -- path/to/file.ts
```

or:

```bash
git diff origin/main...HEAD --unified=1 -- path/to/file.ts
```

---

## 4. Ultra token-efficient mode

Show only hunks with almost no context:

```bash
git diff --cached -U0
```

or:

```bash
git diff origin/main...HEAD -U0
```

---

## 5. Optional: Show function context only

Useful for large files:

```bash
git diff --function-context --unified=1
```

---

## 6. Exclude files

Examples:

```bash
git diff --cached . ':(exclude)package-lock.json'
```

```bash
git diff origin/main...HEAD . ':(exclude)dist'
```

---

## 7. Recommended workflow for Copilot

### Step 1

```bash
git diff --cached --stat
```

### Step 2

```bash
git diff --cached -U0
```

### Step 3

Inspect only problematic files in more detail:

```bash
git diff --cached --unified=1 -- path/to/file
```

---

## 8. Recommended defaults

Recommended:

```bash
git config --global diff.context 1
```

Optional:

```bash
git config --global pager.diff false
```

---

## 9. Important for AI reviews

Prefer:

- `--stat`
- `--name-only`
- `-U0`
- `--unified=1`

Avoid:

- complete file outputs
- large context windows
- `git show`
- unnecessary logs
