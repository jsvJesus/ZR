package ui.components
{
   import com.dvalimona.components.*;
   import com.greensock.*;
   import com.greensock.easing.*;
   import communication.*;
   import flash.display.*;
   import flash.events.*;
   import flash.utils.*;
   import lang.*;
   import logging.*;
   import ui.*;
   
   public class NewPremiumPanel extends NewPanelWithIcon
   {
      private var rejectPremium:ClearButton;
      
      private var currentPremiumCaption:LabelShadowed;
      
      private var currentPremiumDescription:TextShadowed;
      
      private var currentPremiumElapsed:LabelShadowed;
      
      private var currentPremiumPS:TextShadowed;
      
      private var currentPremiumBox:VBox;
      
      private var havePremiumView:Sprite;
      
      private var currentPremiumIcon:Sprite;
      
      private var boosterIcon:Sprite;
      
      private var details:ClearButton;
      
      public function NewPremiumPanel(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0)
      {
         Premium.core.addEventListener(Premium.UPDATED,this.onPremiumUpdated);
         super(parent,xpos,ypos);
         PremiumIcons.addEventListener(Event.COMPLETE,this.onUpdateIcon);
      }
      
      protected function onPremiumUpdated(event:Event) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"NewPremiumPanel.onPremiumUpdated");
         this.updateView();
         this.mouseChildren = true;
      }
      
      protected function onUpdateIcon(event:Event) : void
      {
         this.updateView();
      }
      
      override protected function addChildren() : void
      {
         super.addChildren();
         this.havePremiumView = new Sprite();
         this.havePremiumView.addChild(this.currentPremiumIcon = new Sprite());
         this.currentPremiumBox = new VBox(this.havePremiumView);
         this.currentPremiumBox.spacing = 1;
         this.currentPremiumBox.alignment = VBox.LEFT;
         this.currentPremiumBox.debug = false;
         this.currentPremiumCaption = new LabelShadowed(this.currentPremiumBox);
         this.currentPremiumCaption.color = Style.GOLD_OVER;
         this.currentPremiumCaption.size = 22;
         this.currentPremiumCaption.font = Base.fontName;
         this.currentPremiumCaption.autoSize = true;
         this.currentPremiumDescription = new TextShadowed(this.currentPremiumBox);
         this.currentPremiumDescription.color = 16777215;
         this.currentPremiumDescription.size = 20;
         this.currentPremiumDescription.selectable = false;
         this.currentPremiumDescription.editable = false;
         this.currentPremiumDescription.leading = -5;
         this.currentPremiumDescription.debug = false;
         this.currentPremiumDescription.paddingLeft = 113;
         this.currentPremiumDescription.paddingTop = 15;
         this.currentPremiumElapsed = new LabelShadowed(this.currentPremiumBox);
         this.currentPremiumElapsed.color = 12895428;
         this.currentPremiumElapsed.size = 17;
         this.currentPremiumElapsed.font = Base.lightFontName;
         this.currentPremiumElapsed.paddingTop = 0;
         this.currentPremiumElapsed.paddingLeft = 113;
         this.currentPremiumPS = new TextShadowed(this.currentPremiumBox);
         this.currentPremiumPS.color = 12895428;
         this.currentPremiumPS.size = 17;
         this.currentPremiumPS.selectable = false;
         this.currentPremiumPS.editable = false;
         this.currentPremiumPS.font = Base.lightFontName;
         this.currentPremiumPS.debug = false;
         this.currentPremiumPS.paddingBottom = -20;
         this.currentPremiumPS.paddingLeft = 113;
         this.currentPremiumPS.leading = -5;
         this.rejectPremium = new ClearButton(this.havePremiumView);
         this.rejectPremium.visible = true;
         this.rejectPremium.paddingRight = 10;
         this.rejectPremium.autoWidth = true;
         this.rejectPremium.align = Label.RIGHT;
         this.rejectPremium.size = 18;
         this.rejectPremium.labelUpColor = 5553663;
         this.rejectPremium.label = Locale.getById("extendedGUI.PremiumPanel.rejectPremium");
         this.rejectPremium.addEventListener(MouseEvent.CLICK,this.onRejectPremiumClick);
         this.details = new ClearButton();
         this.details.$ = "extendedGUI.PremiumPanel.details";
         this.details.size = 18;
         this.details.height = 32;
         this.details.addEventListener(MouseEvent.CLICK,this.onDetailsClick);
         this.boosterIcon = new Sprite();
         this.boosterIcon.y = 65;
         this.boosterIcon.x = 25;
         this.boosterIcon.visible = false;
         var ic:Bitmap = new Bitmap(new BoosterIcon(),"auto",true);
         this.boosterIcon.addChild(ic);
         this.addChild(this.boosterIcon);
         this.updateView();
         setTimeout(this.updateView,500);
         setTimeout(this.updateView,1000);
         setTimeout(this.updateView,2000);
         this.mouseChildren = false;
      }
      
      protected function onDetailsClick(event:MouseEvent) : void
      {
         Base.navigator.showDialog(Premium.Current.caption,Premium.Current.description,false,[new DialogButtonItem("extendedGUI.Dialogs.Ok",null,1)],500,350);
      }
      
      override protected function onCaptionClick(event:MouseEvent) : void
      {
         Api.call(Api.SHOW_PREMIUM_SHOP);
      }
      
      protected function onShowPremiumClick(event:MouseEvent) : void
      {
         var choosePremium:ChoosePremiumWindow = new ChoosePremiumWindow(Base.navigator.premiumDialogs);
      }
      
      protected function onChoicePremiumClick(event:MouseEvent) : void
      {
         var choosePremium:ChoosePremiumWindow = new ChoosePremiumWindow(Base.navigator.premiumDialogs);
      }
      
      protected function onRejectPremiumClick(event:MouseEvent) : void
      {
         Base.navigator.showDialog("extendedGUI.Dialogs.Warning","extendedGUI.Dialogs.askRejectPrem",true,[new DialogButtonItem("extendedGUI.Dialogs.Ok",this.onRejectOkButton,0.61),new DialogButtonItem("extendedGUI.Dialogs.Cancel",null,0.39)]);
      }
      
      protected function onRejectOkButton() : *
      {
         Api.call(Api.REJECT_PREM);
      }
      
      protected function get havePremiums() : Boolean
      {
         return Premium.Current != null;
      }
      
      protected function get haveBooster() : Boolean
      {
         return Premium.boosterCurrent != 0;
      }
      
      private function getTimeToEnd(time:Number, postfix:String = "") : String
      {
         var days:uint = 0;
         var real_days:uint = 0;
         var hours:uint = 0;
         var minutes:uint = 0;
         var result:String = "";
         var timeToRemain:uint = time;
         real_days = timeToRemain / (60 * 60 * 24);
         days = Math.ceil(timeToRemain / (60 * 60 * 24));
         hours = timeToRemain / (60 * 60);
         minutes = timeToRemain / 60;
         if(real_days >= 1)
         {
            result = Locale.getById("extendedGUI.PremiumPanel.days") + " " + Locale.getById("extendedGUI.PremiumPanel.toEnd") + " " + postfix + ": " + days;
         }
         else if(hours >= 1)
         {
            result = Locale.getById("extendedGUI.PremiumPanel.hours") + " " + Locale.getById("extendedGUI.PremiumPanel.toEnd") + " " + postfix + ": " + hours;
         }
         else
         {
            result = Locale.getById("extendedGUI.PremiumPanel.minutes") + " " + Locale.getById("extendedGUI.PremiumPanel.toEnd") + " " + postfix + ": " + minutes;
         }
         return result;
      }
      
      override public function set icon(value:Bitmap) : *
      {
      }
      
      public function updateBoosterIcon() : *
      {
         for(var i:* = this.boosterIcon.numChildren - 1; i >= 0; i--)
         {
            this.boosterIcon.removeChildAt(i);
         }
         if(!this.haveBooster)
         {
            return;
         }
         var ico:Bitmap = BoosterIcons.byId(Premium.boosterCurrent,this.havePremiums);
         if(ico == null)
         {
            return;
         }
         this.boosterIcon.addChild(ico);
         ico.x = (138 - ico.width) / 2;
      }
      
      override public function updateView() : void
      {
         var i:* = undefined;
         var ico:Bitmap = null;
         Logger.LogToChannel(Logger.DEBUG,"NewPremiumPanel.updateView",this.havePremiums);
         this.boosterIcon.visible = this.haveBooster;
         caption.visible = true;
         description.visible = true;
         this.currentPremiumDescription.visible = true;
         this.currentPremiumCaption.visible = true;
         this.currentPremiumElapsed.visible = true;
         this.currentPremiumPS.visible = true;
         this.rejectPremium.visible = true;
         this.boosterIcon.y = 50;
         this.boosterIcon.x = 0;
         if(this.havePremiums)
         {
            if(this.contains(this.standartView))
            {
               this.removeChild(standartView);
            }
            if(!this.contains(this.havePremiumView))
            {
               this.addChild(this.havePremiumView);
            }
            this.currentPremiumBox.x = 20;
            this.currentPremiumBox.y = 14;
            if(Premium.boosterCurrent > 0)
            {
               this.currentPremiumCaption.text = Locale.getById("extendedGUI.PremiumPanel.premiumCaption") + Premium.boosterName;
            }
            else
            {
               this.currentPremiumCaption.text = Premium.Current.caption;
            }
            if(Premium.Current.description.length < Premium.MAX_DESCRIPTION_LENGTH)
            {
               this.currentPremiumDescription.text = Premium.Current.description;
               if(this.contains(this.details))
               {
                  this.removeChild(this.details);
               }
            }
            else
            {
               this.currentPremiumDescription.text = Premium.Current.description.substr(0,Premium.MAX_DESCRIPTION_LENGTH) + "…";
               if(!this.contains(this.details))
               {
                  this.addChild(this.details);
               }
            }
            this.currentPremiumElapsed.text = this.getTimeToEnd(Premium.Elapsed,Locale.getById("extendedGUI.PremiumPanel.premiumtoEnd"));
            if(Premium.boosterCurrent > 0)
            {
               this.currentPremiumElapsed.text += "\n" + this.getTimeToEnd(Premium.boosterElapsed,Locale.getById("extendedGUI.PremiumPanel.boostertoEnd"));
            }
            this.rejectPremium.visible = true;
            if(Premium.Current.holiday)
            {
               this.currentPremiumPS.text = Locale.getById("extendedGUI.PremiumPanel.freezeText");
               if(Premium.frizeID != -1)
               {
                  this.currentPremiumPS.text += Locale.getById("extendedGUI.PremiumPanel.freezeText1");
               }
            }
            else
            {
               this.currentPremiumPS.text = "";
            }
            this.currentPremiumCaption.autoSize = false;
            this.currentPremiumDescription.width = preferredWidth - standartBox.x - 60 - 60;
            this.currentPremiumCaption.width = preferredWidth - standartBox.x - 10 - 0;
            this.currentPremiumPS.width = preferredWidth - this.currentPremiumBox.x - 10 - 0;
            for(i = this.currentPremiumIcon.numChildren - 1; i >= 0; i--)
            {
               this.currentPremiumIcon.removeChildAt(i);
            }
            ico = Premium.Current.icon;
            this.currentPremiumIcon.addChild(ico);
            ico.x = (138 - ico.width) / 2;
            caption.visible = false;
            this.currentPremiumDescription.visible = true;
            this.setChildIndex(this.havePremiumView,0);
            this.setChildIndex(back,0);
            setTimeout(this.tuneHeight,100,false);
            setTimeout(this.tuneHeight,400,false);
         }
         else
         {
            if(!this.contains(standartView))
            {
               this.addChild(standartView);
            }
            if(this.contains(this.havePremiumView))
            {
               this.removeChild(this.havePremiumView);
            }
            for(i = standartIcon.numChildren - 1; i >= 0; i--)
            {
               standartIcon.removeChildAt(i);
            }
            standartBox.x = 160 - 62;
            standartBox.y = 40;
            ico = PremiumIcons.NO_PREMIUM_ICON;
            standartIcon.addChild(ico);
            ico.x = (138 - ico.width) / 2;
            description.width = preferredWidth - standartBox.x - 30 - 0;
            description.text = Locale.getById("extendedGUI.RootWindow.premiumHint1");
            if(this.haveBooster)
            {
               description.text += "\n" + this.getTimeToEnd(Premium.boosterElapsed,Locale.getById("extendedGUI.PremiumPanel.boostertoEnd"));
            }
            standartIcon.visible = !this.haveBooster;
            caption.visible = true;
            setTimeout(this.tuneHeight,100,true);
            setTimeout(this.tuneHeight,400,true);
         }
         if(minMode)
         {
            this.currentPremiumBox.y = 9;
            standartBox.y = 35;
            caption.visible = false;
            description.visible = false;
            this.currentPremiumDescription.text = "";
            this.currentPremiumCaption.visible = false;
            this.currentPremiumElapsed.visible = false;
            this.currentPremiumPS.visible = false;
            this.rejectPremium.visible = false;
            ico.x = (128 - ico.width) / 2;
            this.boosterIcon.y = 19;
            this.boosterIcon.x = -4;
         }
         this.updateBoosterIcon();
         invalidate();
      }
      
      override protected function tuneHeight(flag:Boolean) : void
      {
         var th:Number = NaN;
         var tw:Number = preferredWidth;
         description.paddingTop = 18;
         description.size = 20;
         if(flag)
         {
            th = Math.max(standartIcon.y + 105 + 10,description.y + description.height + 10);
            if(minMode)
            {
               th = 128;
               tw = 128;
            }
            TweenMax.to(this,0.8,{
               "height":th,
               "width":tw,
               "ease":Expo.easeOut
            });
         }
         else
         {
            th = Math.max(this.currentPremiumBox.y + this.currentPremiumElapsed.y + this.currentPremiumElapsed.height + 20,this.currentPremiumIcon.y + 105 + 10);
            if(minMode)
            {
               th = 128;
               tw = 128;
            }
            TweenMax.to(this,0.8,{
               "height":th,
               "width":tw,
               "ease":Expo.easeOut
            });
         }
      }
      
      override public function draw() : void
      {
         super.draw();
         this.currentPremiumIcon.y = this.standartIcon.y;
         this.rejectPremium.x = this.width - this.rejectPremium.width - 20;
         this.rejectPremium.y = 18;
         back.width = width;
         back.height = height;
         this.details.x = this.width - this.details.width;
         this.details.y = this.height - this.details.height - 3;
      }
      
      override protected function onMouseOver(e:MouseEvent) : *
      {
         if(is_over)
         {
            return;
         }
         is_over = true;
         if(minMode)
         {
            minMode = false;
         }
         if(this.havePremiums)
         {
            return;
         }
         PlaySounds.onOver();
         back.backColor = 3947580;
         caption.over = true;
      }
      
      override protected function onMouseOut(e:MouseEvent) : *
      {
         super.onMouseOut(e);
      }
      
      override protected function onMouseClick(e:MouseEvent) : *
      {
         if(this.havePremiums)
         {
            return;
         }
         super.onMouseClick(e);
      }
   }
}

