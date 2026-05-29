using System;
using FluidHTN;
using FluidHTN.Operators;

public static class GreyboxDomain
{
    public static Domain<AiContext> Build()
    {
        return new DomainBuilder<AiContext>("Greybox Monster Domain", new FluidHTN.Factory.DefaultFactory())
            .Select("Main Behavior")
                // ========================================================
                // NHÁNH 1 (ƯU TIÊN CAO NHẤT): COMBAT ĐỘNG (DYNAMIC COMBAT)
                // ========================================================
                .Select("Combat State")
                    .Condition("Has Target", ctx => ctx.HasState(AIWorldState.HasTarget))

                    // NÚT THẮT QUYẾT ĐỊNH: ĐÁNH HAY VÂY RÁP?
                    .Select("Attack Or Prowl")

                        // LỰA CHỌN 1: LAO VÀO TẤN CÔNG
                        .Sequence("Engage")
                            .Condition("Has Combat Token", ctx => ctx.HasState(AIWorldState.HasCombatToken))

                            // [ĐÃ FIX]: Chỉ dùng 1 Operator duy nhất để gọi Controller thực thi!
                            .Action("Evaluate And Execute Best Skill")
                                .DoOperator(new UtilityCombatOperator())
                            .End()

                            .Action("Cooldown").DoOperator(new WaitOperator(ctx => ctx.Agent.TacticalSettings.tauntDuration))
                                .Effect("Return Token", EffectType.PlanAndExecute, (ctx, type) => ctx.SetState(AIWorldState.HasCombatToken, false, type))
                            .End()
                        .End()

                        // LỰA CHỌN 2: LƯỢN LỜ UY HIẾP (PROWL)
                        .Sequence("Prowl & Wait")
                            .Action("Circle Player").DoOperator(new CircleTargetOperator(ctx => ctx.Agent.TacticalSettings.circleRadius)).End()
                        .End()

                    .End()
                .End() // Đóng nhánh Combat

                // ========================================================
                // NHÁNH 2: SEARCH (HÙNG HỤC SỤC SẠO KHI MẤT DẤU)
                // ========================================================
                .Sequence("Search Lost Target")
                    .Condition("Is Searching", ctx => ctx.HasState(AIWorldState.IsSearching))

                    .Action("Run To Last Known Position").DoOperator(new MoveToVectorDestinationOperator(true, ctx => ctx.Agent.TacticalSettings.approachDistance)).End()
                    .Action("Search Area Aggressively")
                        .DoOperator(new ScanAreaOperator(ctx => ctx.Agent.TacticalSettings.searchDuration))
                        .Effect("Cooldown To Patrol", EffectType.PlanAndExecute, (ctx, type) =>
                        {
                            ctx.SetState(AIWorldState.IsSearching, false, type);
                            ctx.SetState(AIWorldState.HasSuspectedLocation, false, type);
                        })
                    .End()
                .End()

                // ========================================================
                // NHÁNH 3: INVESTIGATE (CÓ TIẾNG ĐỘNG + ĐƯỢC PHÁT VÉ ĐIỀU TRA)
                // ========================================================
                .Sequence("Investigate - Leader")
                    .Condition("Has Suspicion", ctx => ctx.HasState(AIWorldState.HasSuspectedLocation))
                    .Condition("Has Investigate Token", ctx => ctx.HasState(AIWorldState.HasInvestigateToken))

                    .Action("Telegraph Suspicion").DoOperator(new TelegraphOperator(true, ctx => ctx.Agent.TacticalSettings.windUpTime)).End()
                    .Action("Turn Body").DoOperator(new TurnToFaceOperator(true, ctx => ctx.Agent.TacticalSettings.turnSpeed)).End()
                    .Action("Tactical Approach").DoOperator(new MoveToVectorDestinationOperator(true, ctx => ctx.Agent.TacticalSettings.approachDistance)).End()
                    .Action("Search Area")
                        .DoOperator(new ScanAreaOperator(ctx => ctx.Agent.TacticalSettings.scanDuration))
                        .Effect("Return Token", EffectType.PlanAndExecute, (ctx, type) =>
                        {
                            ctx.SetState(AIWorldState.HasSuspectedLocation, false, type);
                            ctx.SetState(AIWorldState.HasInvestigateToken, false, type);
                        })
                    .End()
                .End()

                // ========================================================
                // NHÁNH 4: ALERT / OVERWATCH (CÓ BIẾN NHƯNG BỊ TỪ CHỐI VÉ - ĐỨNG NHÌN)
                // ========================================================
                .Sequence("Investigate - Bystander")
                    .Condition("Has Suspicion", ctx => ctx.HasState(AIWorldState.HasSuspectedLocation))
                    .Condition("No Investigate Token", ctx => !ctx.HasState(AIWorldState.HasInvestigateToken))

                    .Action("Look At Disturbance").DoOperator(new TelegraphOperator(true, ctx => ctx.Agent.TacticalSettings.windUpTime)).End()
                    .Action("Turn To Face").DoOperator(new TurnToFaceOperator(true, ctx => ctx.Agent.TacticalSettings.turnSpeed)).End()

                    .Action("Hold Position & Watch")
                        .DoOperator(new TelegraphOperator(true, ctx => ctx.Agent.TacticalSettings.overwatchTime))
                        .Effect("Clear Local Suspicion", EffectType.PlanAndExecute, (ctx, type) =>
                        {
                            ctx.SetState(AIWorldState.HasSuspectedLocation, false, type);
                        })
                    .End()
                .End()

                // ========================================================
                // ========================================================
                // NHÁNH 5 (ƯU TIÊN THẤP NHẤT): PATROL (FALLBACK ĐI TUẦN)
                // ========================================================
                .Sequence("Patrol Methodically")
                    .Action("Get Next Waypoint").DoOperator(new GetNextWaypointOperator()).End()
                    .Action("Look At Waypoint").DoOperator(new TelegraphOperator(false, ctx => ctx.Agent.TacticalSettings.tauntDuration)).End()
                    .Action("Turn To Waypoint").DoOperator(new TurnToFaceOperator(false, ctx => ctx.Agent.TacticalSettings.turnSpeed)).End()
                    .Action("Move To Waypoint").DoOperator(new MoveToVectorDestinationOperator(false, ctx => 0.5f)).End()
                    // VI: Sửa số 3f ở đây / EN: Fix the 3f right here:
                    .Action("Rest And Scan").DoOperator(new ScanAreaOperator(ctx => ctx.Agent.TacticalSettings.patrolRestTime)).End()
                .End()

            .End()
            .Build();
    }
}

public static class DomainBuilderExtensions
{
    public static DomainBuilder<AiContext> DoOperator(this DomainBuilder<AiContext> builder, IOperator op)
    {
        if (builder.Pointer is FluidHTN.PrimitiveTasks.IPrimitiveTask task)
        {
            task.SetOperator(op);
        }
        return builder;
    }
}