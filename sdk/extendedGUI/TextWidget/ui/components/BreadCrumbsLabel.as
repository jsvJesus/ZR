package ui.components
{
   import com.dvalimona.components.*;
   import com.greensock.*;
   import com.greensock.easing.*;
   import communication.*;
   import events.*;
   import flash.display.DisplayObjectContainer;
   import flash.events.*;
   import flash.text.*;
   import flash.utils.*;
   
   public class BreadCrumbsLabel extends Component
   {
      private var textFieldsByCrumbs:Dictionary;
      
      private var crumbsByTextFields:Dictionary;
      
      private var itemsIDs:Array;
      
      private var itemToDelete:TextField;
      
      private var itemToAdd:TextField;
      
      private var activeItem:TextField;
      
      private var activeAlpha:Number = 1;
      
      private var inactiveAlpha:Number = 0.5;
      
      private var activeScale:Number = 1;
      
      private var inactiveScale:Number = 0.5;
      
      private var overAlpha:Number = 1;
      
      private var overColor:uint = 10347511;
      
      private var outColor:uint = 16777215;
      
      public function BreadCrumbsLabel(param1:DisplayObjectContainer = null, param2:Number = 0, param3:Number = 0)
      {
         this.textFieldsByCrumbs = new Dictionary();
         this.crumbsByTextFields = new Dictionary();
         this.itemsIDs = new Array();
         BreadCrumbs.self.addEventListener(BreadCrumbEvent.ADDED,this.onBreadCrumbsAdded);
         BreadCrumbs.self.addEventListener(BreadCrumbEvent.REMOVED,this.onBreadCrumbsRemoved);
         BreadCrumbs.self.addEventListener(BreadCrumbEvent.CHANGED,this.onBreadCrumbsChanged);
         super(param1,param2,param3);
      }
      
      protected function onBreadCrumbsChanged(param1:BreadCrumbEvent) : void
      {
      }
      
      protected function onBreadCrumbsRemoved(param1:BreadCrumbEvent) : void
      {
         this.removeItem(param1.data);
      }
      
      protected function onBreadCrumbsAdded(param1:BreadCrumbEvent) : void
      {
         this.addItem(param1.data);
      }
      
      private function createItem() : TextField
      {
         var _loc1_:TextField = new TextField();
         _loc1_.defaultTextFormat = new TextFormat(Style.boldFontName,30,16777215);
         _loc1_.autoSize = TextFieldAutoSize.LEFT;
         _loc1_.borderColor = 16777215;
         _loc1_.border = false;
         _loc1_.embedFonts = false;
         this.addChild(_loc1_);
         _loc1_.addEventListener(MouseEvent.CLICK,this.onClick);
         return _loc1_;
      }
      
      protected function onClick(param1:MouseEvent) : void
      {
         this.goToItem(param1.target as TextField);
      }
      
      protected function goToItem(param1:TextField) : void
      {
      }
      
      protected function onOut(param1:MouseEvent) : void
      {
         var _loc2_:TextField = param1.target as TextField;
         TweenMax.to(_loc2_,0.3,{
            "alpha":(_loc2_ == this.activeItem ? this.activeAlpha : this.inactiveAlpha),
            "ease":Expo.easeOut
         });
      }
      
      protected function onOver(param1:MouseEvent) : void
      {
         var _loc2_:TextField = param1.target as TextField;
         TweenMax.to(_loc2_,0.5,{
            "alpha":this.overAlpha,
            "ease":Expo.easeOut
         });
      }
      
      private function addItem(param1:BreadCrumb) : void
      {
         var _loc2_:TextField = this.createItem();
         this.textFieldsByCrumbs[param1] = _loc2_;
         this.crumbsByTextFields[_loc2_] = param1;
         _loc2_.text = param1.screen.label;
         this.itemToAdd = _loc2_;
         this.updateLabelsView();
         invalidate();
      }
      
      private function isExist(param1:BreadCrumb) : Boolean
      {
         var _loc2_:Boolean = false;
         var _loc3_:uint = 0;
         while(_loc3_ < this.numChildren)
         {
            if(this.textFieldsByCrumbs[param1] != null)
            {
               _loc2_ = true;
            }
            _loc3_++;
         }
         return _loc2_;
      }
      
      private function removeItem(param1:BreadCrumb) : void
      {
         var _loc2_:TextField = this.textFieldsByCrumbs[param1] as TextField;
         if(_loc2_ != null)
         {
            this.itemToDelete = _loc2_;
            setTimeout(this.killItem,1000,_loc2_);
            this.updateLabelsView();
            invalidate();
         }
      }
      
      private function killItem(param1:TextField) : void
      {
         delete this.crumbsByTextFields[param1];
         this.removeChild(param1);
      }
      
      private function updateLabelsView() : void
      {
         var _loc1_:TextField = null;
         var _loc3_:uint = 0;
         var _loc2_:Array = [];
         _loc3_ = 0;
         while(_loc3_ < this.numChildren)
         {
            _loc1_ = this.getChildAt(_loc3_) as TextField;
            if(_loc1_ == this.itemToDelete)
            {
               this.activeItem = _loc2_.pop();
            }
            else if(_loc1_ == this.itemToAdd)
            {
               this.activeItem = _loc1_;
            }
            else
            {
               _loc2_.push(_loc1_);
            }
            _loc3_++;
         }
         _loc3_ = 0;
         while(_loc3_ < _loc2_.length)
         {
            _loc1_ = _loc2_[_loc3_] as TextField;
            TweenMax.to(_loc1_,1,{
               "alpha":0.5,
               "y":14,
               "scaleX":this.inactiveScale,
               "scaleY":this.inactiveScale,
               "ease":Expo.easeInOut,
               "onUpdate":this.alignItems
            });
            _loc3_++;
         }
         if(this.activeItem != null)
         {
            if(this.activeItem == this.itemToAdd)
            {
               this.activeItem.alpha = 0;
            }
            TweenMax.to(this.activeItem,1,{
               "alpha":1,
               "y":0,
               "z":0,
               "scaleX":this.activeScale,
               "scaleY":this.activeScale,
               "ease":Expo.easeInOut,
               "onUpdate":this.alignItems
            });
         }
         if(this.itemToDelete != null)
         {
            TweenMax.to(this.itemToDelete,0.5,{
               "alpha":0,
               "ease":Expo.easeInOut,
               "onUpdate":this.alignItems
            });
         }
         this.itemToDelete = null;
         this.itemToAdd = null;
      }
      
      private function alignItems() : void
      {
         var _loc1_:Number = NaN;
         var _loc3_:TextField = null;
         var _loc2_:Number = 0;
         var _loc4_:uint = 0;
         while(_loc4_ < this.numChildren)
         {
            _loc3_ = this.getChildAt(_loc4_) as TextField;
            _loc3_.x = _loc2_;
            _loc2_ += _loc3_.width + 5;
            _loc4_++;
         }
      }
      
      override public function draw() : void
      {
         this.alignItems();
      }
   }
}

