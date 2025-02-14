﻿using System;
using Rage;
using LSPD_First_Response.Mod.Callouts;
using LSPD_First_Response.Mod.API;
using System.Drawing;
using System.Collections.Generic;
using UnitedCallouts.Stuff;

namespace UnitedCallouts.Callouts
{
    [CalloutInfo("[UC] Suspicious ATM Activity", CalloutProbability.Medium)]
    public class SuspiciousATMActivity : Callout
    {
        private string[] wepList = new string[] { "WEAPON_PISTOL", "WEAPON_CROWBAR", "WEAPON_KNIFE" };
        public Vector3 _SpawnPoint;
        public Vector3 _searcharea;
        public Blip _Blip;
        public Ped _Aggressor;
        private bool _hasBegunAttacking = false;
        private bool _hasPursuitBegun = false;
        private LHandle _pursuit;
        private bool _pursuitCreated = false;
        private int _scenario = 0;

        public override bool OnBeforeCalloutDisplayed()
        {
            Random random = new Random();
            List<Vector3> list = new List<Vector3>();
            Tuple<Vector3, float>[] SpawningLocationList =
            {
                Tuple.Create(new Vector3(112.7427f, -818.8912f, 31.33836f),161.2627f),
                Tuple.Create(new Vector3(-203.61f, -861.6489f, 30.26763f),25.38977f),
                Tuple.Create(new Vector3(288.3719f, -1282.444f, 29.65594f),278.5002f),
                Tuple.Create(new Vector3(-526.4995f, -1222.698f, 18.45498f),151.1967f),
                Tuple.Create(new Vector3(-821.5978f, -1082.233f, 11.13243f),32.33837f),
                Tuple.Create(new Vector3(-618.8591f, -706.7742f, 30.05278f),270.148f),

        };
            for (int i = 0; i < SpawningLocationList.Length; i++)
            {
                list.Add(SpawningLocationList[i].Item1);
            }
            int num = LocationChooser.nearestLocationIndex(list);
            _SpawnPoint = SpawningLocationList[num].Item1;
            _Aggressor = new Ped(_SpawnPoint, SpawningLocationList[num].Item2);
            _scenario = new Random().Next(0, 100);
            ShowCalloutAreaBlipBeforeAccepting(_SpawnPoint, 15f);
            CalloutMessage = "[UC]~w~ Reports of Suspicious ATM Activity.";
            CalloutPosition = _SpawnPoint;
            Functions.PlayScannerAudioUsingPosition("ATTENTION_ALL_UNITS CRIME_SUSPICIOUS_ACTIVITY_01 IN_OR_ON_POSITION", _SpawnPoint);
            // Functions.PlayScannerAudioUsingPosition("ATTENTION_ALL_UNITS IN_OR_ON_POSITION", _SpawnPoint);
            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            Game.LogTrivial("UnitedCallouts Log: ATMActivity callout accepted.");
            Game.DisplayNotification("web_lossantospolicedept", "web_lossantospolicedept", "~w~UnitedCallouts", "~y~Suspicious ATM Activity", "~b~Dispatch: ~w~Someone called the police because of suspicious activitiy at an ATM. Respond with ~r~Code 3");

            _Aggressor.IsPersistent = true;
            _Aggressor.BlockPermanentEvents = true;
            _Aggressor.Armor = 200;
            _Aggressor.Inventory.GiveNewWeapon(new WeaponAsset(wepList[new Random().Next((int)wepList.Length)]), 500, true);

            _searcharea = _SpawnPoint.Around2D(1f, 2f);
            _Blip = new Blip(_searcharea, 20f);
            _Blip.Color = Color.Yellow;
            _Blip.EnableRoute(Color.Yellow);
            _Blip.Alpha = 0.5f;
            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            if (_Aggressor) _Aggressor.Delete();
            if (_Blip) _Blip.Delete();
            base.OnCalloutNotAccepted();
        }

        public override void Process()
        {
            GameFiber.StartNew(delegate
            {
                if (_Aggressor && _Aggressor.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 20f && !_hasBegunAttacking)
                {
                    if (_scenario > 40)
                    {
                        new RelationshipGroup("AG");
                        new RelationshipGroup("VI");
                        _Aggressor.RelationshipGroup = "AG";
                        Game.LocalPlayer.Character.RelationshipGroup = "VI";
                        Game.SetRelationshipBetweenRelationshipGroups("AG", "VI", Relationship.Hate);
                        _Aggressor.Tasks.FightAgainstClosestHatedTarget(1000f);
                        GameFiber.Wait(200);
                        _Aggressor.Tasks.FightAgainst(Game.LocalPlayer.Character);
                        _hasBegunAttacking = true;
                        GameFiber.Wait(600);
                    }
                    else
                    {
                        if (!_hasPursuitBegun)
                        {
                            _pursuit = Functions.CreatePursuit();
                            Functions.AddPedToPursuit(_pursuit, _Aggressor);
                            Functions.SetPursuitIsActiveForPlayer(_pursuit, true);
                            _pursuitCreated = true;
                            _hasPursuitBegun = true;
                        }
                    }
                }
                if (_Aggressor && _Aggressor.IsDead) End();
                if (Functions.IsPedArrested(_Aggressor)) End();
                if (Game.LocalPlayer.Character.IsDead) End();
                if (Game.IsKeyDown(Settings.EndCall)) End();
            }, "Suspicious ATM Activity [UnitedCallouts]");
            base.Process();
        }

        public override void End()
        {
            if (_Blip) _Blip.Delete();
            if (_Aggressor) _Aggressor.Dismiss();
            Game.DisplayNotification("web_lossantospolicedept", "web_lossantospolicedept", "~w~UnitedCallouts", "~y~Suspicious ATM Activity", "~b~You: ~w~Dispatch we're code 4. Show me ~g~10-8.");
            Functions.PlayScannerAudio("ATTENTION_THIS_IS_DISPATCH_HIGH ALL_UNITS_CODE4 NO_FURTHER_UNITS_REQUIRED");
            base.End();
        }
    }
}
