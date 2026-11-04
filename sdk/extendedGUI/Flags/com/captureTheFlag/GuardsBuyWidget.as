package com.captureTheFlag
{
   import com.GameCommunication;
   import flash.display.MovieClip;
   import flash.events.MouseEvent;
   import flash.external.ExternalInterface;
   import flash.text.TextField;
   import scaleform.clik.controls.Button;
   
   public class GuardsBuyWidget extends MovieClip
   {
      public var GuardLabel:TextField;
      
      public var PatrolLabel:TextField;
      
      public var SniperLabel:TextField;
      
      public var GuardianText:TextField;
      
      public var BuyGuardian:Button;
      
      public var StopBuyGuard:Button;
      
      public var removeBtn:Button;
      
      public var PatrolText:TextField;
      
      public var BuyPatrol:Button;
      
      public var SniperText:TextField;
      
      public var BuySniper:Button;
      
      protected var _currentCount:int = 0;
      
      protected var _maxCount:int = 6;
      
      protected var _patrolCount:int = 0;
      
      protected var _patrolMaxCount:int = 0;
      
      protected var _sniperCount:int = 0;
      
      protected var _sniperMaxCount:int = 0;
      
      protected var _stateBuying:Boolean = false;
      
      public function GuardsBuyWidget()
      {
         super();
         ExternalInterface.addCallback("start_guardians_buying",this.startBuying);
         ExternalInterface.addCallback("deactivate_guardians_buying",this.deactivateBuying);
         ExternalInterface.addCallback("activate_guardians_buying",this.activateBuying);
         ExternalInterface.addCallback("on_buy_guard",this.onBuyedGuard);
         ExternalInterface.addCallback("stop_guardians_buying",this.stopBuying);
         ExternalInterface.addCallback("on_change_guard_count",this.updateGuardCount);
         this.BuyGuardian.addEventListener(MouseEvent.CLICK,this.onBuyGuard);
         this.BuyPatrol.addEventListener(MouseEvent.CLICK,this.onBuyPatrol);
         this.BuySniper.addEventListener(MouseEvent.CLICK,this.onBuySniper);
         this.StopBuyGuard.addEventListener(MouseEvent.CLICK,this.onStopBuy);
         this.removeBtn.addEventListener(MouseEvent.CLICK,this.onRemoveGuardians);
      }
      
      private function updateGuardCount(param1:*) : *
      {
         this._currentCount = param1.simple_guard.current_count;
         this._maxCount = param1.simple_guard.max_count;
         this._patrolCount = param1.patrol_guard.current_count;
         this._patrolMaxCount = param1.patrol_guard.max_count;
         this._sniperCount = param1.sniper_guard.current_count;
         this._sniperMaxCount = param1.sniper_guard.max_count;
         this.updateText();
      }
      
      private function onBuyGuard(param1:MouseEvent) : *
      {
         this.disable_buttons();
         GameCommunication.post_guardian(0);
      }
      
      private function onBuyPatrol(param1:MouseEvent) : *
      {
         this.disable_buttons();
         GameCommunication.post_guardian(1);
      }
      
      private function onBuySniper(param1:MouseEvent) : *
      {
         this.disable_buttons();
         GameCommunication.post_guardian(2);
      }
      
      private function onStopBuy(param1:MouseEvent) : *
      {
         this.stopBuying();
         GameCommunication.stop_guardians_buying();
      }
      
      private function onRemoveGuardians(param1:MouseEvent) : *
      {
         GameCommunication.remove_guardians();
      }
      
      protected function onBuyedGuard(param1:*) : *
      {
         if(param1)
         {
            this.updateText();
         }
         if(this._stateBuying)
         {
            this.BuyGuardian.enabled = true;
            this.BuyPatrol.enabled = true;
            this.BuySniper.enabled = true;
         }
      }
      
      protected function disable_buttons() : *
      {
         this.BuySniper.selected = false;
         this.BuySniper.focused = 0;
         this.BuySniper.enabled = false;
         this.BuyPatrol.selected = false;
         this.BuyPatrol.focused = 0;
         this.BuyPatrol.enabled = false;
         this.BuyGuardian.selected = false;
         this.BuyGuardian.focused = 0;
         this.BuyGuardian.enabled = false;
      }
      
      protected function enable_buttons() : *
      {
         this.BuyGuardian.enabled = true;
         this.BuyPatrol.enabled = true;
         this.BuySniper.enabled = true;
      }
      
      private function activateBuying() : *
      {
         this._stateBuying = true;
         this.BuyGuardian.enabled = true;
         this.BuyPatrol.enabled = true;
         this.BuySniper.enabled = true;
      }
      
      private function deactivateBuying() : *
      {
         this._stateBuying = false;
         this.BuyGuardian.enabled = false;
         this.BuySniper.enabled = false;
      }
      
      private function updateText() : *
      {
         this.GuardianText.text = Object(root).base.MainFlags.Loc.AVAILABLE + " " + this._currentCount + " / " + this._maxCount;
         this.PatrolText.text = Object(root).base.MainFlags.Loc.AVAILABLE + " " + this._patrolCount + " / " + this._patrolMaxCount;
         this.SniperText.text = Object(root).base.MainFlags.Loc.AVAILABLE + " " + this._sniperCount + " / " + this._sniperMaxCount;
      }
      
      private function startBuying(param1:*) : *
      {
         Object(root).stopResize();
         this.GuardLabel.text = Object(root).base.MainFlags.Loc.GUARDS;
         this.PatrolLabel.text = Object(root).base.MainFlags.Loc.PATROL;
         this.SniperLabel.text = Object(root).base.MainFlags.Loc.SNIPER;
         this.enable_buttons();
         this.StopBuyGuard.label = Object(root).base.MainFlags.Loc.STOP_BUY;
         this.removeBtn.label = Object(root).base.MainFlags.Loc.REMOVE_GUARDSY;
         this.visible = true;
         this.y = Object(root).base.MainFlags.y + Object(root).base.MainFlags.SubClockField.y * Object(root).base.MainFlags.scaleY + Object(root).base.MainFlags.SubClockField.height * Object(root).base.MainFlags.scaleY + 10;
         this._currentCount = param1.simple_guard.current_count;
         this._maxCount = param1.simple_guard.max_count;
         this._patrolCount = param1.patrol_guard.current_count;
         this._patrolMaxCount = param1.patrol_guard.max_count;
         this._sniperCount = param1.sniper_guard.current_count;
         this._sniperMaxCount = param1.sniper_guard.max_count;
         this.updateText();
      }
      
      private function stopBuying() : *
      {
         Object(root).startResize();
         this.visible = false;
         this.StopBuyGuard.selected = false;
         this.BuyGuardian.enabled = false;
      }
   }
}

