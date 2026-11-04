package ui.components
{
   import com.dvalimona.components.*;
   import com.greensock.*;
   import com.greensock.easing.*;
   import communication.*;
   import flash.display.DisplayObjectContainer;
   import flash.events.*;
   import flash.ui.*;
   import flash.utils.*;
   import lang.*;
   import logging.*;
   
   public class ChoosePremiumWindow extends Component
   {
      protected static const HeaderHeight:uint = 50;
      
      protected static const ButtonsHeight:uint = 70;
      
      protected static const ItemHeight:uint = 150;
      
      protected static const NumVisibleItemsMax:uint = 3;
      
      protected var topBox:HBox;
      
      protected var headerLabel:LabelShadowed;
      
      protected var list:PremiumList;
      
      protected var bottomBox:HBox;
      
      protected var buy:PushButton;
      
      protected var exit:PushButton;
      
      protected var back:Diagrid;
      
      public function ChoosePremiumWindow(param1:DisplayObjectContainer = null, param2:Number = 0, param3:Number = 0)
      {
         Premium.core.addEventListener(Premium.SUCCESS,this.onPremiumSuccess);
         Premium.core.addEventListener(Premium.FAIL,this.onPremiumFail);
         Base.stage.addEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
         Base.navigator.premiumShield.show(0.9);
         this.tuneSize();
         super(param1,param2,param3);
      }
      
      private function tuneSize() : void
      {
         var _loc1_:uint = HeaderHeight + 1 + ButtonsHeight + ItemHeight * (Premium.list.length > NumVisibleItemsMax ? NumVisibleItemsMax : Premium.list.length);
         this.setSize(800,_loc1_);
      }
      
      override protected function unfreeze() : void
      {
         this.alpha = 0;
         this.x = (Base.stage.stageWidth - this.width) / 2;
         this.y = (Base.stage.stageHeight - this.height) / 2 + 100;
         TweenMax.to(this,0.5,{
            "alpha":1,
            "y":(Base.stage.stageHeight - this.height) / 2,
            "ease":Expo.easeOut
         });
         Base.stage.focus = this;
         Dummy.visible = false;
      }
      
      override protected function addChildren() : void
      {
         super.addChildren();
         this.addEventListener(MouseEvent.MOUSE_DOWN,this.onMouseGoDown);
         this.addChild(this.back = new Diagrid());
         this.headerDeals();
         this.bodyDeals();
      }
      
      private function headerDeals() : void
      {
         this.topBox = new HBox(this);
         this.topBox.mouseEnabled = false;
         this.topBox.mouseChildren = false;
         this.topBox.fixedHeight = HeaderHeight;
         this.topBox.alignment = HBox.MIDDLE;
         this.topBox.horizontalAlign = HBox.LEFT;
         this.topBox.shift = 2;
         this.topBox.debug = false;
         this.headerLabel = new LabelShadowed(this.topBox);
         this.headerLabel.mouseEnabled = this.headerLabel.mouseChildren = false;
         this.headerLabel.font = Base.FONT_BOLD;
         this.headerLabel.size = 22;
         this.headerLabel.paddingLeft = 15;
         this.headerLabel.text = Premium.Current == null ? Locale.getById("extendedGUI.PremiumPanel.choosePremium") : Locale.getById("extendedGUI.PremiumPanel.premiumList");
      }
      
      private function bodyDeals() : void
      {
         this.list = new PremiumList(this);
         this.list.addEventListener(Event.SELECT,this.onPremiumSelected);
         this.list.items = Premium.list;
         this.bottomBox = new HBox(this);
         this.bottomBox.fixedHeight = ButtonsHeight;
         this.bottomBox.alignment = HBox.MIDDLE;
         this.bottomBox.horizontalAlign = HBox.LEFT;
         this.bottomBox.spacing = 1;
         this.bottomBox.debug = true;
         this.buy = new PushButton(this.bottomBox);
         this.buy.autoWidth = false;
         this.buy.label = this.buyButtonText;
         this.buy.height = ButtonsHeight;
         this.buy.addEventListener(MouseEvent.CLICK,this.onBuyClick);
         this.buy.font = Base.FONT_BOLD;
         this.exit = new PushButton(this.bottomBox);
         this.exit.autoWidth = false;
         this.exit.$ = "extendedGUI.PremiumPanel.exitButton";
         this.exit.height = ButtonsHeight;
         this.exit.addEventListener(MouseEvent.CLICK,this.onExitClick);
         this.updateBuyButton();
      }
      
      protected function onPremiumSelected(param1:Event) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onPremiumSelected",this.list.selectedItem.caption);
         this.updateBuyButton();
      }
      
      protected function onBuyClick(param1:MouseEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onBuyClick",this.list.selectedItem.id);
         if(this.list.selectedItem != null && this.list.selectedItem.id != null)
         {
            this.buy.enabled = false;
            Premium.Enable(this.list.selectedItem.id);
         }
         else
         {
            Logger.LogToChannel(Logger.ERROR,"onBuyClick","list.selectedItem.id ~ null");
         }
      }
      
      protected function onPremiumSuccess(param1:Event) : void
      {
         this.buy.enabled = false;
         Premium.core.removeEventListener(Premium.SUCCESS,this.onPremiumSuccess);
         Premium.core.removeEventListener(Premium.FAIL,this.onPremiumFail);
         this.doClose();
      }
      
      protected function onPremiumFail(param1:Event) : void
      {
         this.buy.enabled = false;
         Premium.core.removeEventListener(Premium.SUCCESS,this.onPremiumSuccess);
         Premium.core.removeEventListener(Premium.FAIL,this.onPremiumFail);
         this.doClose();
      }
      
      protected function onExitClick(param1:MouseEvent) : void
      {
         this.doClose();
      }
      
      private function doClose() : void
      {
         Dummy.visible = true;
         Base.navigator.premiumShield.hide();
         var _loc1_:TimelineMax = new TimelineMax();
         _loc1_.staggerFromTo([this,this.back,this.topBox,this.list,this.bottomBox].reverse(),0.3,{
            "alpha":1,
            "z":0,
            "ease":Expo.easeOut
         },{
            "alpha":0,
            "z":500,
            "ease":Expo.easeOut
         },0.05,"+=0",this.onShowOffComplete);
      }
      
      private function onShowOffComplete() : void
      {
         setTimeout(this.parent.removeChild,0,this);
         setTimeout(this.nullMePlease,10);
      }
      
      private function nullMePlease() : void
      {
         Base.stage.focus = Base.navigator.currentScreen;
      }
      
      protected function updateBuyButton() : void
      {
         this.buy.label = this.buyButtonText;
         this.buy.enabled = this.list.selectedItem != null && Premium.Current == null;
      }
      
      protected function get buyButtonText() : String
      {
         var _loc1_:* = null;
         if(this.list.selectedItem != null)
         {
            if(Premium.Current == null)
            {
               _loc1_ = Locale.getById("extendedGUI.PremiumPanel.buy") + " «" + this.list.selectedItem.caption + "» " + Locale.getById("extendedGUI.PremiumPanel.for") + " " + this.list.selectedItem.price + " " + Locale.getById("extendedGUI.PremiumPanel.gold");
            }
            else
            {
               _loc1_ = Locale.getById("extendedGUI.PremiumPanel.alreadyHavePremium") + " «" + Premium.Current.caption + "»";
            }
         }
         else
         {
            _loc1_ = Premium.Current == null ? Locale.getById("extendedGUI.PremiumPanel.buy") : Locale.getById("extendedGUI.PremiumPanel.alreadyHavePremium") + " «" + Premium.Current.caption + "»";
         }
         return _loc1_;
      }
      
      protected function onMouseGoDown(param1:MouseEvent) : void
      {
         var event:MouseEvent = param1;
         Logger.LogToChannel(Logger.DEBUG,this,"onMouseGoDown",event.localY,event.target == this.back);
         if(event.localY < HeaderHeight && event.target == this.back)
         {
            try
            {
               Logger.LogToChannel(Logger.DEBUG,"startDrag..");
               this.startDrag();
               stage.addEventListener(MouseEvent.MOUSE_UP,this.onMouseGoUp);
            }
            catch(error:Error)
            {
               Logger.LogToChannel(Logger.ERROR,"startDrag",error);
            }
         }
      }
      
      protected function onMouseGoUp(param1:MouseEvent) : void
      {
         this.stopDrag();
         stage.removeEventListener(MouseEvent.MOUSE_UP,this.onMouseGoUp);
      }
      
      protected function onKeyDown(param1:KeyboardEvent) : void
      {
         switch(param1.keyCode)
         {
            case Keyboard.ESCAPE:
            case Keyboard.BACKSPACE:
               this.doClose();
         }
      }
      
      override public function draw() : void
      {
         super.draw();
         this.back.width = this.width;
         this.back.height = this.height;
         this.topBox.x = 0;
         this.topBox.y = 0;
         this.list.x = 1;
         this.list.y = HeaderHeight;
         this.list.width = this.width - 1;
         this.list.height = this.height - HeaderHeight - ButtonsHeight - 1 - 1;
         this.list.listItemHeight = 150;
         this.exit.width = 180;
         this.buy.width = this.width - this.exit.width - 1 - 1 - 1;
         this.bottomBox.width = this.width - 2;
         this.bottomBox.x = 1;
         this.bottomBox.y = this.height - ButtonsHeight - 1;
      }
   }
}

