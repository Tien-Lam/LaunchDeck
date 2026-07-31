import { describe, expect, test } from "bun:test";
import { readFileSync } from "node:fs";

const loadWorkflow = (name: string) =>
  Bun.YAML.parse(
    readFileSync(new URL(`../workflows/${name}`, import.meta.url), "utf8"),
  ) as Record<string, any>;

const stepByName = (job: Record<string, any>, name: string) =>
  job.steps.find((step: Record<string, any>) => step.name === name);

const semanticVersion =
  /^v(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)(?:-(?:0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*)(?:\.(?:0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*))*)?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$/;

const isSemanticVersion = (value: string) =>
  value.match(semanticVersion)?.[0] === value;

describe("release workflow trust boundary", () => {
  test("accepts only exact lowercase-v ASCII semantic versions", () => {
    const valid = [
      "v0.0.0",
      "v1.2.3",
      "v1.2.3-rc.1",
      "v1.2.3+build.5",
      "v1.2.3-rc.1+build.5",
    ];
    const invalid = [
      "V1.2.3",
      "v1.2.3\n",
      "v1٢.0.0",
      "v१.2.3",
      "v01.2.3",
      "v1.2.3-01",
      "v1.2.3-alpha..1",
    ];

    expect(valid.every(isSemanticVersion)).toBe(true);
    expect(invalid.some(isSemanticVersion)).toBe(false);
  });

  test("keeps the historical tag workflow inert", () => {
    const workflow = loadWorkflow("release.yml");

    expect(workflow.on.push).toBeUndefined();
    expect(workflow.on.workflow_dispatch).toBeDefined();
    expect(workflow.permissions).toEqual({});
    expect(workflow.jobs.retired).toBeDefined();
  });

  test("keeps release requests read-only", () => {
    const workflow = loadWorkflow("release-request.yml");
    const request = workflow.jobs.request;
    const validation = stepByName(request, "Validate release request");
    const upload = stepByName(request, "Upload release request");

    expect(workflow.on.workflow_dispatch.inputs.version.required).toBe(true);
    expect(workflow.permissions).toEqual({ contents: "read" });
    expect(validation.run).toContain("refs/heads/$env:DEFAULT_BRANCH");
    expect(validation.run).toContain("[0-9]");
    expect(validation.run).not.toContain("\\d");
    expect(validation.run).toContain("\\A");
    expect(validation.run).toContain("\\z");
    expect(validation.run).toContain("-cnotmatch");
    expect(upload.with["retention-days"]).toBe(1);
  });

  test("publishes only through trusted workflow_run code and gated jobs", () => {
    const workflow = loadWorkflow("publish-release.yml");
    const jobs = workflow.jobs;
    const verify = jobs["verify-release"];
    const build = jobs["build-msix"];
    const publish = jobs["publish-release"];
    const verification = stepByName(verify, "Verify trusted main request");
    const targetRecheck = stepByName(publish, "Recheck release target");
    const tagCreation = stepByName(
      publish,
      "Create release tag at the verified SHA",
    );

    expect(workflow.on.workflow_run.workflows).toEqual(["Release Request"]);
    expect(workflow.permissions).toEqual({
      actions: "read",
      contents: "read",
    });
    expect(verify.if).toContain(
      "github.event.workflow_run.head_branch == github.event.repository.default_branch",
    );
    expect(verification.run).toContain(
      "$env:REQUESTED_SHA -ne $mainSha",
    );
    expect(verification.run).toContain("[0-9]");
    expect(verification.run).not.toContain("\\d");
    expect(verification.run).toContain("\\A");
    expect(verification.run).toContain("\\z");
    expect(verification.run).toContain("-cnotmatch");

    expect(build.needs).toBe("verify-release");
    expect(build.strategy.matrix.platform).toEqual(["x64", "ARM64"]);
    expect(build.uses).toBe("./.github/workflows/build-msix.yml");
    expect(build.with.configuration).toBe("Release");

    expect(publish.needs).toEqual(["verify-release", "build-msix"]);
    expect(publish.permissions).toEqual({
      actions: "read",
      contents: "write",
    });
    expect(targetRecheck.run).toContain("$env:RELEASE_SHA -ne $mainSha");
    expect(tagCreation.with.script).toContain("github.rest.git.createRef");
    expect(tagCreation.with.script).toContain(
      "sha: process.env.RELEASE_SHA",
    );
    expect(
      publish.steps.findIndex(
        (step: Record<string, any>) =>
          step.name === "Create architecture-specific release archives",
      ),
    ).toBeLessThan(
      publish.steps.findIndex(
        (step: Record<string, any>) => step.name === "Recheck release target",
      ),
    );
    expect(
      stepByName(publish, "Confirm main after tag creation").with.script,
    ).toContain("branch.data.commit.sha !== process.env.RELEASE_SHA");
    expect(stepByName(publish, "Clean up a failed release").if).toContain(
      "steps.github-release.outcome != 'success'",
    );
    expect(
      stepByName(publish, "Clean up a failed release").with.script,
    ).toContain("github.rest.git.deleteRef");

    const writeJobs = Object.values(jobs)
      .filter((job: any) => job.permissions?.contents === "write")
      .map((job: any) => job.name);
    expect(writeJobs).toEqual(["publish-release"]);
  });
});
