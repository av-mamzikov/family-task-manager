-- Test script for FamilyTaskManager Worker
-- Run this script to create test data and verify Worker functionality

-- ============================================================================
-- 1. CREATE TEST DATA
-- ============================================================================

-- Get existing family and pet (or create new ones)
DO $$
DECLARE
    v_family_id UUID;
    v_pet_id UUID;
    v_user_id UUID;
BEGIN
    -- Get or create test user
    SELECT "Id" INTO v_user_id FROM "Users" LIMIT 1;
    IF v_user_id IS NULL THEN
        INSERT INTO "Users" ("Id", "TelegramId", "Name", "CreatedAt")
        VALUES (gen_random_uuid(), 999999999, 'Test User', NOW())
        RETURNING "Id" INTO v_user_id;
        RAISE NOTICE 'Created test user: %', v_user_id;
    END IF;

    -- Get or create test family
    SELECT "Id" INTO v_family_id FROM "Families" LIMIT 1;
    IF v_family_id IS NULL THEN
        INSERT INTO "Families" ("Id", "Name", "CreatedAt", "Timezone", "LeaderboardEnabled")
        VALUES (gen_random_uuid(), 'Test Family', NOW(), 'UTC', true)
        RETURNING "Id" INTO v_family_id;
        RAISE NOTICE 'Created test family: %', v_family_id;

        -- Add user as admin
        INSERT INTO "FamilyMembers" ("Id", "UserId", "FamilyId", "Role", "Points", "JoinedAt", "IsActive")
        VALUES (gen_random_uuid(), v_user_id, v_family_id, 0, 0, NOW(), true);
    END IF;

    -- Get or create test pet
    SELECT "Id" INTO v_pet_id FROM "Pets" WHERE "FamilyId" = v_family_id LIMIT 1;
    IF v_pet_id IS NULL THEN
        INSERT INTO "Pets" ("Id", "FamilyId", "Type", "Name", "MoodScore", "CreatedAt")
        VALUES (gen_random_uuid(), v_family_id, 0, 'Мурзик', 50, NOW())
        RETURNING "Id" INTO v_pet_id;
        RAISE NOTICE 'Created test pet: %', v_pet_id;
    END IF;

    -- Create test TaskTemplate (runs every 2 minutes for testing)
    INSERT INTO "TaskTemplates" (
        "Id", 
        "FamilyId", 
        "PetId", 
        "Title", 
        "Points", 
        "Schedule", 
        "CreatedBy", 
        "CreatedAt", 
        "IsActive"
    )
    VALUES (
        gen_random_uuid(),
        v_family_id,
        v_pet_id,
        'Покормить кота (тест)',
        10,
        '0 */2 * * * ?',  -- Every 2 minutes for testing
        v_user_id,
        NOW(),
        true
    );
    RAISE NOTICE 'Created test TaskTemplate';

    -- Create another TaskTemplate (runs every 3 minutes)
    INSERT INTO "TaskTemplates" (
        "Id", 
        "FamilyId", 
        "PetId", 
        "Title", 
        "Points", 
        "Schedule", 
        "CreatedBy", 
        "CreatedAt", 
        "IsActive"
    )
    VALUES (
        gen_random_uuid(),
        v_family_id,
        v_pet_id,
        'Поиграть с котом (тест)',
        15,
        '0 */3 * * * ?',  -- Every 3 minutes for testing
        v_user_id,
        NOW(),
        true
    );
    RAISE NOTICE 'Created second test TaskTemplate';
END $$;

-- ============================================================================
-- 2. VERIFICATION QUERIES
-- ============================================================================

-- Check TaskTemplates
SELECT 
    "Id",
    "Title",
    "Points",
    "Schedule",
    "IsActive",
    "CreatedAt"
FROM "TaskTemplates"
ORDER BY "CreatedAt" DESC;

-- Check Pets
SELECT 
    "Id",
    "Name",
    "Type",
    "MoodScore",
    "CreatedAt"
FROM "Pets"
ORDER BY "CreatedAt" DESC;

-- ============================================================================
-- 3. MONITOR WORKER EXECUTION
-- ============================================================================

-- Wait 2-3 minutes after starting Worker, then run these queries:

-- Check TaskInstances created by Worker
SELECT 
    ti."Id",
    ti."Title",
    ti."Status",
    ti."Points",
    ti."DueAt",
    ti."CreatedAt",
    tt."Schedule",
    CASE 
        WHEN ti."Status" = 0 THEN 'Active'
        WHEN ti."Status" = 1 THEN 'InProgress'
        WHEN ti."Status" = 2 THEN 'Completed'
    END as "StatusText"
FROM "TaskInstances" ti
LEFT JOIN "TaskTemplates" tt ON ti."TemplateId" = tt."Id"
ORDER BY ti."CreatedAt" DESC
LIMIT 10;

-- Check Quartz job execution
SELECT 
    "SCHED_NAME",
    "JOB_NAME",
    "JOB_GROUP",
    "DESCRIPTION"
FROM "QRTZ_JOB_DETAILS"
ORDER BY "JOB_NAME";

-- Check Quartz triggers
SELECT 
    "TRIGGER_NAME",
    "TRIGGER_STATE",
    to_timestamp("NEXT_FIRE_TIME" / 1000) as "NextFireTime",
    to_timestamp("PREV_FIRE_TIME" / 1000) as "PrevFireTime"
FROM "QRTZ_TRIGGERS"
ORDER BY "TRIGGER_NAME";

-- ============================================================================
-- 4. TEST PET MOOD CALCULATION
-- ============================================================================

-- Create some completed tasks to test mood calculation
DO $$
DECLARE
    v_family_id UUID;
    v_pet_id UUID;
    v_user_id UUID;
    v_task_id UUID;
BEGIN
    SELECT "Id" INTO v_family_id FROM "Families" LIMIT 1;
    SELECT "Id" INTO v_pet_id FROM "Pets" WHERE "FamilyId" = v_family_id LIMIT 1;
    SELECT "Id" INTO v_user_id FROM "Users" LIMIT 1;

    -- Create completed task (on time)
    INSERT INTO "TaskInstances" (
        "Id", "FamilyId", "PetId", "Title", "Points", "Type", 
        "Status", "CompletedBy", "CompletedAt", "CreatedAt", "DueAt"
    )
    VALUES (
        gen_random_uuid(),
        v_family_id,
        v_pet_id,
        'Completed on time',
        20,
        0, -- OneTime
        2, -- Completed
        v_user_id,
        NOW() - INTERVAL '1 hour',
        NOW() - INTERVAL '2 hours',
        NOW() - INTERVAL '30 minutes'
    );

    -- Create overdue task (not completed)
    INSERT INTO "TaskInstances" (
        "Id", "FamilyId", "PetId", "Title", "Points", "Type", 
        "Status", "CreatedAt", "DueAt"
    )
    VALUES (
        gen_random_uuid(),
        v_family_id,
        v_pet_id,
        'Overdue task',
        15,
        0, -- OneTime
        0, -- Active
        NOW() - INTERVAL '2 days',
        NOW() - INTERVAL '1 day'
    );

    RAISE NOTICE 'Created test tasks for mood calculation';
END $$;

-- Check pet mood before calculation
SELECT "Id", "Name", "MoodScore" FROM "Pets";

-- Wait for PetMoodCalculatorJob to run (30 minutes or change schedule to */2 for testing)
-- Then check again:
-- SELECT "Id", "Name", "MoodScore" FROM "Pets";

-- ============================================================================
-- 5. CLEANUP TEST DATA
-- ============================================================================

-- Uncomment to clean up test data:
/*
DELETE FROM "TaskInstances" WHERE "Title" LIKE '%тест%' OR "Title" LIKE '%test%';
DELETE FROM "TaskTemplates" WHERE "Title" LIKE '%тест%' OR "Title" LIKE '%test%';
-- DELETE FROM "Pets" WHERE "Name" = 'Мурзик';
-- DELETE FROM "FamilyMembers" WHERE "UserId" IN (SELECT "Id" FROM "Users" WHERE "Name" = 'Test User');
-- DELETE FROM "Families" WHERE "Name" = 'Test Family';
-- DELETE FROM "Users" WHERE "Name" = 'Test User';
*/

-- ============================================================================
-- 6. USEFUL MONITORING QUERIES
-- ============================================================================

-- Count TaskInstances by status
SELECT 
    CASE 
        WHEN "Status" = 0 THEN 'Active'
        WHEN "Status" = 1 THEN 'InProgress'
        WHEN "Status" = 2 THEN 'Completed'
    END as "Status",
    COUNT(*) as "Count"
FROM "TaskInstances"
GROUP BY "Status";

-- Recent TaskInstances with template info
SELECT 
    ti."Title",
    ti."Status",
    ti."Points",
    ti."DueAt",
    ti."CreatedAt",
    tt."Schedule",
    p."Name" as "PetName"
FROM "TaskInstances" ti
LEFT JOIN "TaskTemplates" tt ON ti."TemplateId" = tt."Id"
LEFT JOIN "Pets" p ON ti."PetId" = p."Id"
WHERE ti."CreatedAt" > NOW() - INTERVAL '1 hour'
ORDER BY ti."CreatedAt" DESC;

-- Pet mood scores
SELECT 
    p."Name",
    p."Type",
    p."MoodScore",
    COUNT(ti."Id") as "TotalTasks",
    COUNT(CASE WHEN ti."Status" = 2 THEN 1 END) as "CompletedTasks",
    COUNT(CASE WHEN ti."Status" != 2 AND ti."DueAt" < NOW() THEN 1 END) as "OverdueTasks"
FROM "Pets" p
LEFT JOIN "TaskInstances" ti ON ti."PetId" = p."Id"
GROUP BY p."Id", p."Name", p."Type", p."MoodScore";

-- Family statistics
SELECT 
    f."Name" as "FamilyName",
    COUNT(DISTINCT fm."UserId") as "Members",
    COUNT(DISTINCT p."Id") as "Pets",
    COUNT(DISTINCT tt."Id") as "TaskTemplates",
    COUNT(ti."Id") as "TaskInstances"
FROM "Families" f
LEFT JOIN "FamilyMembers" fm ON fm."FamilyId" = f."Id" AND fm."IsActive" = true
LEFT JOIN "Pets" p ON p."FamilyId" = f."Id"
LEFT JOIN "TaskTemplates" tt ON tt."FamilyId" = f."Id" AND tt."IsActive" = true
LEFT JOIN "TaskInstances" ti ON ti."FamilyId" = f."Id"
GROUP BY f."Id", f."Name";
