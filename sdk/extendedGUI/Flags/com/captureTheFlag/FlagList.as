package com.captureTheFlag
{
   import flash.display.MovieClip;
   
   public class FlagList extends MovieClip
   {
      protected const ind_distns:* = 13;
      
      protected var _flags:Object = new Object();
      
      protected var _sortedArrayFlagNames:Array = new Array();
      
      public function FlagList()
      {
         super();
      }
      
      public function addFlag(param1:CircleFlagIndicator) : *
      {
         this._flags[param1.flagName] = param1;
      }
      
      public function buyedFlag(param1:String, param2:Boolean) : *
      {
         if(this._flags[param1] != null)
         {
            if(param2)
            {
               this._flags[param1].value = 100;
               this._flags[param1].myFlag = true;
               this._flags[param1].onStatePlay();
            }
            else
            {
               this._flags[param1].value = 0;
               this._flags[param1].myFlag = true;
               this._flags[param1].addBuy();
            }
         }
      }
      
      public function ByedFlags(param1:Object) : *
      {
         var _loc2_:* = undefined;
         this._flags = new Object();
         this._sortedArrayFlagNames = new Array();
         for(_loc2_ in param1)
         {
            if(_loc2_ != "flag_cost")
            {
               this._flags[_loc2_] = new CircleFlagIndicator();
               this._flags[_loc2_].flagName = _loc2_;
               if(param1[_loc2_].active)
               {
                  this._flags[_loc2_].value = 100;
                  this._flags[_loc2_].myFlag = true;
                  this._flags[_loc2_].addShow();
               }
               else
               {
                  this._flags[_loc2_].value = 0;
                  this._flags[_loc2_].myFlag = true;
                  this._flags[_loc2_].addBuy();
                  this._flags[_loc2_].addShow();
               }
               this._sortedArrayFlagNames.push(_loc2_);
            }
         }
         this._sortedArrayFlagNames.sort();
         this.drawFlags();
      }
      
      public function flagsData(param1:Object) : *
      {
         var _loc2_:* = undefined;
         this._flags = new Object();
         this._sortedArrayFlagNames = new Array();
         for(_loc2_ in param1)
         {
            this._flags[_loc2_] = new CircleFlagIndicator();
            this._flags[_loc2_].flagName = _loc2_;
            this._flags[_loc2_].value = param1[_loc2_].height;
            this._flags[_loc2_].myFlag = param1[_loc2_].is_my_flag;
            this._flags[_loc2_].am_i_invader = param1[_loc2_].am_i_invader;
            this._sortedArrayFlagNames.push(_loc2_);
         }
         this._sortedArrayFlagNames.sort();
         this.drawFlags();
      }
      
      public function setFlagValue(param1:String, param2:Number) : *
      {
         if(this._flags[param1] != null)
         {
            this._flags[param1].value = param2;
         }
         else
         {
            this.addFlagByName(param1,param2);
         }
      }
      
      public function setFlagOwner(param1:String, param2:Boolean, param3:Boolean) : *
      {
         if(this._flags[param1] != null)
         {
            this._flags[param1].myFlag = param2;
            this._flags[param1].am_i_invader = param3;
         }
         else
         {
            this.addFlagByName(param1,0,param2);
         }
      }
      
      protected function addFlagByName(param1:String, param2:Number = -1, param3:Boolean = false) : *
      {
         this._flags[param1] = new CircleFlagIndicator();
         this._flags[param1].flagName = param1;
         this._flags[param1].value = param2;
         this._flags[param1].myFlag = param3;
         this._sortedArrayFlagNames.push(param1);
         this._sortedArrayFlagNames.sort();
         this.drawFlags();
      }
      
      protected function drawFlags() : *
      {
         var _loc5_:* = undefined;
         var _loc6_:String = null;
         var _loc1_:* = numChildren - 1;
         while(_loc1_ >= 0)
         {
            removeChildAt(_loc5_);
            _loc1_--;
         }
         var _loc2_:Number = this.ind_distns;
         var _loc3_:Number = 0;
         var _loc4_:int = 0;
         for(_loc5_ in this._sortedArrayFlagNames)
         {
            _loc6_ = this._sortedArrayFlagNames[_loc5_].toString();
            this._flags[_loc6_].y = _loc2_;
            this._flags[_loc6_].x = _loc3_;
            _loc3_ += 64;
            this.addChild(this._flags[_loc6_]);
            _loc4_++;
            if(_loc4_ == 3)
            {
               _loc2_ += 50 + this.ind_distns;
               _loc3_ = 0;
               _loc4_ = 0;
            }
         }
      }
   }
}

